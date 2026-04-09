using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application;

/// <summary>
/// Defines the contract for processing a crawl job in the crawling pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface are responsible for validating a <see cref="CrawlJob"/>, <br />
/// fetching the corresponding resource using a crawler and returning the results of the crawl.
/// </para>
/// </remarks>
public interface IProcessCrawlJobUseCase
{
	/// <summary>
	/// Executes the crawl process for a specific job, coordinating fetching, change detection, and indexing.
	/// </summary>
	/// <param name="job">The crawl job containing the target URL and scheduling metadata.</param>
	/// <returns>
	/// A <see cref="ProcessJobResponse"/> indicating whether content was updated 
	/// and containing the result of the crawl attempt.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown if the job or URL is invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the fetch attempt fails to produce a result.</exception>
	public Task<ProcessJobResponse> Execute(CrawlJob job);  
}
