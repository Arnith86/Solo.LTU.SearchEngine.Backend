using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace LTU.SearchEngine.BackgroundServices;

/// <summary>
/// TPL Dataflow-based implementation of <see cref="ICrawlJobDispatcher"/> responsible for <br />
/// scheduling, prioritizing, retrying, and executing crawl jobs asynchronously.
/// </summary>
/// <remarks>
/// This dispatcher supports **hot-swappable configuration** by utilizing an <br />
/// <see cref="ICrawlerSettingsLoader"/> to fetch the latest settings during runtime.
/// This dispatcher provides:
/// <list type="bullet">
///   <item><description>Priority-based scheduling using <see cref="PriorityQueue{TElement,TPriority}"/></description></item>
///   <item><description>Asynchronous execution using TPL Dataflow pipelines</description></item>
///   <item><description>Retry handling with configurable retry intervals</description></item>
///   <item><description>Per-domain concurrency throttling using semaphores</description></item>
///   <item><description>Failure handling and rescheduling logic</description></item>
/// </list>
/// 
/// The class acts as the orchestration layer between crawl job scheduling and
/// the processing use-case, providing a scalable and testable execution model.
/// </remarks>
public class TplCrawlJobDispatcher : ICrawlJobDispatcher
{
	private readonly IProcessCrawlJobUseCase _processCrawlJobUseCase;
	private readonly ICrawlerSettingsLoader _crawlerSettingsLoader;
	private readonly SemaphoreProvider _semaphoreProvider;
	private PriorityQueue<CrawlJob, DateTime> _jobPriorityQueue;
	private BufferBlock<CrawlJob> _buffer;
	private ActionBlock<CrawlJob> _worker;
	private readonly ILogger<TplCrawlJobDispatcher> _logger;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="TplCrawlJobDispatcher"/> class.
	/// </summary>
	/// <param name="processCrawlJobUseCase">Use case responsible for executing individual crawl jobs.</param>
	/// <param name="crawlerSettingsLoader">Loader used to retrieve hot-swappable crawler settings.</param>
   	/// <param name="semaphoreProvider">Provider responsible for managing per-domain concurrency semaphores.</param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when any dependency is <c>null</c>.
	/// </exception>
	public TplCrawlJobDispatcher(
		IProcessCrawlJobUseCase processCrawlJobUseCase,
		SemaphoreProvider semaphoreProvider,
		ICrawlerSettingsLoader crawlerSettingsLoader,
		ILogger<TplCrawlJobDispatcher> logger
		)
	{
		_processCrawlJobUseCase = processCrawlJobUseCase
			?? throw new ArgumentNullException(nameof(processCrawlJobUseCase));
		_crawlerSettingsLoader = crawlerSettingsLoader
			?? throw new ArgumentNullException(nameof(crawlerSettingsLoader));
		_semaphoreProvider = semaphoreProvider 
			?? throw new ArgumentNullException(nameof(semaphoreProvider)); 
		_logger = logger 
			?? throw new ArgumentNullException(nameof(logger));

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
				_crawlerSettingsLoader.Load().MaxConcurrencyPerDomain
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

		}, 
		new ExecutionDataflowBlockOptions
		{
			MaxDegreeOfParallelism = 100 // ToDo: this should be configurable through app settings and not hard coded.
		});
	}

	private async Task HandleUseCaseAsync(CrawlJob job)
	{
		try
		{
			CrawlResult result = await _processCrawlJobUseCase.Execute(job);
			await EnqueueNewJobs(result);
		}
		catch (ArgumentException ex)
		{
			_logger.LogWarning($"Job {job.Id} skipped: invalid job ({ex.Message})");
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogError($"Job {job.Id} failed: fetch error ({ex.Message})");
			await HandleFailedJob(job);
		}
		catch (Exception ex)
		{
			_logger.LogError($"Job {job.Id} failed with unexpected error: {ex.Message}");
			await HandleFailedJob(job);
		}
	}

	private async Task HandleFailedJob(CrawlJob job)
	{
		if (job.RetryCount >= _crawlerSettingsLoader.Load().RetryIntervals.Count)
		{
			_logger.LogWarning($"Job {job.Id} reached max retry count. Discarding job.");
			return;
		}

		int index = (job.RetryCount - 1) >= 0 ? job.RetryCount - 1 : 0;

		job.NextAttempt = 
			DateTime.UtcNow + _crawlerSettingsLoader.Load().RetryIntervals[index];

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

	/// <inheritdoc />
	public Task Enqueue(CrawlJob job)
	{
		if (job == null) throw new ArgumentNullException(nameof(job));

		// If job is due for immediate processing, send to buffer; otherwise, enqueue with scheduled time.
		if (job.NextAttempt is null)
			return _buffer.SendAsync(job);
		else
		{
			lock (_jobPriorityQueue)
				_jobPriorityQueue.Enqueue(job, job.NextAttempt ?? DateTime.UtcNow);
		}

	
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task Start(CancellationToken ct)
	{
		var initialSetting = _crawlerSettingsLoader.Load();

		_logger.LogInformation(
			"Crawler Dispatcher started. Effective configuration: " +
			"UserAgent={UserAgent}, SeedURLs={SeedUrl}, MaxConcurrencyPerDomain={MaxConcurrency}, " +
			"MinDelayMs={MinDelay}ms, RetryIntervals={RetryIntervals}, WhiteList={WhiteList}",
			initialSetting.UserAgent,
			initialSetting.SeedUrls,
			initialSetting.MaxConcurrencyPerDomain,
			initialSetting.MinDelayMs,
			initialSetting.RetryIntervals,
			initialSetting.WhiteList
		);
		
		_buffer.LinkTo(
			_worker, 
			new DataflowLinkOptions { PropagateCompletion = true }
		);

		try
		{
			while (!ct.IsCancellationRequested)
			{
				DateTime now = DateTime.UtcNow;
				CrawlJob? crawlJob = null;

				lock (_jobPriorityQueue)
				{
					if (_jobPriorityQueue.Count > 0 &&
						_jobPriorityQueue.Peek().NextAttempt <= now)
					{
						crawlJob =  _jobPriorityQueue.Dequeue();	
					}
				}

				if (crawlJob is not null)
				{
					await _buffer.SendAsync(crawlJob, ct);
					await Task.Delay(_crawlerSettingsLoader.Load().MinDelayMs, ct);
				}
				else await Task.Delay(50, ct); // Processor friendly wait
			}		
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Dispatcher has been stopped"); 
		}
      
    }
}
