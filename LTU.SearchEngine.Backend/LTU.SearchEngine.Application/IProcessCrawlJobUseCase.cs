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
	/// Processes a given crawl job by fetching the URL, validating the domain, extracts the contents, <br />
	/// and returning the result of the crawl.
	/// </summary>
	/// <param name="job">The <see cref="CrawlJob"/> to be processed. Must not be <c>null</c> and must contain a valid URL.</param>
	/// <returns>A <see cref="CrawlResult"/> representing the fetched and processed content.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="job"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentException">Thrown if <paramref name="job"/> contains an invalid URL.</exception>
	/// <exception cref="DomainNotWhitelistedException">Thrown if the URL of <paramref name="job"/> is not allowed by the domain validator.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the crawler fails to fetch content for the URL.</exception>
	public Task<CrawlResult> Execute(CrawlJob job);  
}
