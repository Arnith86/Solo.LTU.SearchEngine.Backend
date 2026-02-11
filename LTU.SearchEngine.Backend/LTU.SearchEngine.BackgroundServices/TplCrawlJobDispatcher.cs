using LTU.SearchEngine.Application;
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
	private readonly Dictionary<string, SemaphoreSlim> _domainSemaphores;
	private PriorityQueue<CrawlJob, DateTime> _jobPriorityQueue;
	private BufferBlock<CrawlJob> _buffer;
	private ActionBlock<CrawlJob> _worker;

	public TplCrawlJobDispatcher(
		IProcessCrawlJobUseCase processCrawlJobUseCase,
		CrawlerSettings crawlerSettings
		)
	{
		_processCrawlJobUseCase = processCrawlJobUseCase
			?? throw new ArgumentNullException(nameof(processCrawlJobUseCase));
		_crawlerSettings = crawlerSettings
			?? throw new ArgumentNullException(nameof(crawlerSettings));

		_domainSemaphores = new Dictionary<string, SemaphoreSlim>();

		_jobPriorityQueue = new PriorityQueue<CrawlJob, DateTime>();
		_buffer = new BufferBlock<CrawlJob>();
		_worker = SetupWorker();
	}

	private ActionBlock<CrawlJob> SetupWorker()
	{
		return new ActionBlock<CrawlJob>(async job =>
		{
			string domain = new Uri(job.Url).Host.ToLowerInvariant();
			var semaphore = GetSemaphore(domain);

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
			await _processCrawlJobUseCase.Execute(job);
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
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Job {job.Id} failed with unexpected error: {ex.Message}");
		}
	}

	private SemaphoreSlim GetSemaphore(string domain)
	{
		lock (_domainSemaphores)
		{
			if (!_domainSemaphores.TryGetValue(domain, out var semaphore))
			{
				semaphore = new SemaphoreSlim(_crawlerSettings.MaxConcurrencyPerDomain);
				_domainSemaphores[domain] = semaphore;
			}

			return semaphore;
		}
	}

	public Task Enqueue(CrawlJob job)
	{
		if (job == null) throw new ArgumentNullException(nameof(job));

		// If job is due for immediate processing, send to buffer; otherwise, enqueue with scheduled time.
		if (job.NextAttempt is null || job.NextAttempt <= DateTime.UtcNow)
			_buffer.SendAsync(job);
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

			await Task.Delay(TimeSpan.FromMilliseconds(200));
		}
	}
}
