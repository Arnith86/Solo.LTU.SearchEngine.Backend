using LTU.SearchEngine.Backend.Core.Model;

namespace LTU.SearchEngine.Infrastructure.Crawling;

/// <summary>
/// Defines functionality for fetching and crawling web resources.
/// </summary>
public interface ICrawler
{
    /// <summary>
    /// Fetches the content located at the specified URL and performs <br />
    /// crawling-related processing on the retrieved resource.
    /// </summary>
    /// <param name="url">The URL of the resource to fetch.</param>
    /// <returns>
    /// A <see cref="CrawlResult"/> containing the fetched content, <br />
    /// metadata, and any crawl-related information.
    /// </returns>
    Task<CrawlResult> FetchAsync(string url);
}