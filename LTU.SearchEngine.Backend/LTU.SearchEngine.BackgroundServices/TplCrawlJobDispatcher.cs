using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace LTU.SearchEngine.BackgroundServices;

public class TplCrawlJobDispatcher : ICrawlJobDispatcher
{
	private readonly IProcessCrawlJobUseCase _processCrawlJobUseCase;
	private readonly CrawlerSettings _crawlerSettings;
	private readonly SemaphoreProvider _semaphoreProvider;
	private PriorityQueue<CrawlJob, DateTime> _jobPriorityQueue;
	private BufferBlock<CrawlJob> _buffer;
	private ActionBlock<CrawlJob> _worker;

	public TplCrawlJobDispatcher(
		IProcessCrawlJobUseCase processCrawlJobUseCase,
		CrawlerSettings crawlerSettings,
		SemaphoreProvider semaphoreProvider
		)
	{
		_processCrawlJobUseCase = processCrawlJobUseCase
			?? throw new ArgumentNullException(nameof(processCrawlJobUseCase));
		_crawlerSettings = crawlerSettings
			?? throw new ArgumentNullException(nameof(crawlerSettings));
		_semaphoreProvider = semaphoreProvider 
			?? throw new ArgumentNullException(nameof(semaphoreProvider)); 

		_jobPriorityQueue = new PriorityQueue<CrawlJob, DateTime>();
		_buffer = new BufferBlock<CrawlJob>();
		_worker = SetupWorker();
	}

	private ActionBlock<CrawlJob> SetupWorker()
	{
		return new ActionBlock<CrawlJob>(async job =>
		{
			string domain = new Uri(job.Url).Host.ToLowerInvariant();
			var semaphore = _semaphoreProvider.GetOrAddSemaphore(
				domain, 
				_crawlerSettings.MaxConcurrencyPerDomain
			);

			// This will limit concurrent processing of jobs from the same domain to the configured maximum.
			await semaphore.WaitAsync();
			
			try
			{
				await HandleUseCaseAsync(job);
			}
			finally 
			{
				semaphore.Release();
			}

		});
	}

	// These catches should later be logged properly and not simply written to debug output!
	private async Task HandleUseCaseAsync(CrawlJob job)
	{
		try
		{
			CrawlResult result = await _processCrawlJobUseCase.Execute(job);
			await EnqueueNewJobs(result);
		}
		catch (DomainNotWhitelistedException ex)
		{
			Debug.WriteLine($"Job {job.Id} skipped: domain not whitelisted ({ex.Message})");
		}
		catch (ArgumentException ex)
		{
			Debug.WriteLine($"Job {job.Id} skipped: invalid job ({ex.Message})");
		}
		catch (InvalidOperationException ex)
		{
			Debug.WriteLine($"Job {job.Id} failed: fetch error ({ex.Message})");
			await HandleFailedJob(job);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Job {job.Id} failed with unexpected error: {ex.Message}");
			await HandleFailedJob(job);
		}
	}

	private async Task HandleFailedJob(CrawlJob job)
	{
		if (job.RetryCount >= _crawlerSettings.RetryIntervals.Count)
		{
			Debug.WriteLine($"Job {job.Id} reached max retry count. Discarding job.");
			return;
		}

		int index = (job.RetryCount - 1) >= 0 ? job.RetryCount - 1 : 0;

		job.NextAttempt = 
			DateTime.UtcNow + _crawlerSettings.RetryIntervals[index];

		job.RetryCount++;
		
		await Enqueue(job);
	}

	private async Task EnqueueNewJobs(CrawlResult result)
	{
		foreach (string link in result.ExtractedLinks)
		{
			CrawlJob newJob = new CrawlJob
			{
				Url = link,
				NextAttempt = DateTime.UtcNow
			};

			await Enqueue(newJob);
		}
	}

	public Task Enqueue(CrawlJob job)
	{
		if (job == null) throw new ArgumentNullException(nameof(job));

		// If job is due for immediate processing, send to buffer; otherwise, enqueue with scheduled time.
		if (job.NextAttempt is null || job.NextAttempt <= DateTime.UtcNow)
			return _buffer.SendAsync(job);
		else
		{
			lock (_jobPriorityQueue)
				_jobPriorityQueue.Enqueue(job, job.NextAttempt ?? DateTime.UtcNow);
		}

		return Task.CompletedTask;
	}

	public async Task Start(CancellationToken ct)
	{
		_buffer.LinkTo(
			_worker, 
			new DataflowLinkOptions { PropagateCompletion = true }
		);

		while (!ct.IsCancellationRequested)
		{
			DateTime now = DateTime.UtcNow;

			List<CrawlJob> ready = new List<CrawlJob>();

			lock (_jobPriorityQueue)
			{
				while (_jobPriorityQueue.Count > 0 && 
					_jobPriorityQueue.Peek().NextAttempt <= now) 
				{
					ready.Add(_jobPriorityQueue.Dequeue());
				}
			}

			foreach (CrawlJob job in ready) await _buffer.SendAsync(job, ct);

			await Task.Delay(_crawlerSettings.MinDelayMs);
		}
	}
}
