using LTU.SearchEngine.Backend.Core.Model.Entities;

namespace LTU.SearchEngine.BackgroundServices;

/// <summary>
/// Defines a dispatcher responsible for scheduling, queuing, and executing crawl jobs.
/// </summary>
/// <remarks>
/// This interface abstracts the job orchestration layer and decouples job scheduling <br />
/// from job execution logic, enabling testability, scalability, and extensibility.
/// </remarks>
public interface ICrawlJobDispatcher
{
	/// <summary>
	/// Enqueues a crawl job for execution or scheduling.
	/// </summary>
	/// <param name="job">
	/// The crawl job to enqueue. If <see cref="CrawlJob.NextAttempt"/> is less than or equal to the <br/>
	/// current time, the job is scheduled for immediate execution; otherwise, it is placed <br/>
	/// in a queue for deferred execution.
	/// </param>
	/// <returns>A task representing the asynchronous enqueue operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is <c>null</c>.</exception>
	public Task Enqueue(CrawlJob job);

	/// <summary>
	/// Starts the dispatcher processing loop.
	/// </summary>
	/// <param name="ct">
	/// A cancellation token used to stop the dispatcher loop gracefully.
	/// </param>
	/// <returns>
	/// A task representing the lifetime of the dispatcher loop. The task completes when
	/// cancellation is requested and the dispatcher shuts down.
	/// </returns>
	/// <remarks>
	/// This method starts the internal scheduling and execution pipeline, including:
	/// <list type="bullet">
	///   <item><description>Priority-based job scheduling</description></item>
	///   <item><description>Worker pipeline activation</description></item>
	///   <item><description>Concurrency-limited execution per domain</description></item>
	///   <item><description>Retry and rescheduling mechanisms</description></item>
	/// </list>
	/// 
	/// This method should typically be started once during application startup.
	/// </remarks>
	public Task Start(CancellationToken ct);
}
