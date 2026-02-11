using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Threading.Tasks.Dataflow;

namespace LTU.SearchEngine.BackgroundServices;

public class TplCrawlJobDispatcher : ICrawlJobDispatcher
{
	private readonly IProcessCrawlJobUseCase _processCrawlJobUseCase;
	private readonly CrawlerSettings _crawlerSettings;
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

		_jobPriorityQueue = new PriorityQueue<CrawlJob, DateTime>();
		_buffer = new BufferBlock<CrawlJob>();
		_worker = new ActionBlock<CrawlJob>(async job =>
		{
			await _processCrawlJobUseCase.Execute(job);
		});
	}

	public Task Enqueue(CrawlJob job)
	{
		if (job == null) throw new ArgumentNullException(nameof(job));

		// If job is due for immediate processing, send to buffer; otherwise, enqueue with scheduled time.
		if (job.NextAttempt is null || job.NextAttempt <= DateTime.UtcNow)
			_buffer.SendAsync(job);
		
		lock (_jobPriorityQueue)
			_jobPriorityQueue.Enqueue(job, job.NextAttempt ?? DateTime.UtcNow);

		return Task.CompletedTask;
	}

	public Task Start(CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}
