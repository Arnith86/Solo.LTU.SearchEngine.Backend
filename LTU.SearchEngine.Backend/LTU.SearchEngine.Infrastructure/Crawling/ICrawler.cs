using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Infrastructure.Crawling;

/// <summary>
/// Defines the contract for fetching and processing web content.
/// Supports incremental crawling by separating raw data retrieval from heavy parsing.
/// </summary>
public interface ICrawler
{
    /// <summary>
    /// Performs the initial network request to retrieve raw binary data and metadata.
    /// </summary>
    /// <param name="url">The absolute URL of the resource to fetch.</param>
    /// <returns> 
    /// A <see cref="RawCrawlData"/> containing the raw bytes, status code, and encoding information.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown if the network request fails or returns a non-success status code.
    /// </exception>
    Task<CrawlResult> FetchAsync(RawCrawlData data, string hashedContent);

    /// <summary>
    /// Generates a cryptographic hash of the content to detect changes.
    /// </summary>
    /// <remarks>
    /// For HTML content, this method typically cleans and extracts raw text before hashing 
    /// to avoid "noise" (like timestamps) from triggering unnecessary re-indexing.
    /// </remarks>
    /// <param name="data">The raw data retrieved from <see cref="FetchRawAsync"/>.</param>
    /// <returns>A hexadecimal string representing the content hash.</returns>
    Task<RawCrawlData> FetchRawAsync(string url);

    /// <summary>
    /// Performs full linguistic and structural parsing of the raw data.
    /// </summary>
    /// <remarks>
    /// This is a heavy operation involving term extraction, link discovery, and title parsing.
    /// It should only be invoked if the content hash indicates that the page has changed.
    /// </remarks>
    /// <param name="data">The raw data to parse.</param>
    /// <param name="hashedContent">The pre-calculated hash to include in the final result.</param>
    /// <returns>A comprehensive <see cref="CrawlResult"/> ready for indexing.</returns>
    Task<string> GetContentHash(RawCrawlData data);

    /// <summary>
    /// Creates a standardized error result for failed crawl attempts.
    /// </summary>
    /// <param name="url">The URL where the error occurred.</param>
    /// <param name="statusCode">The HTTP status code associated with the failure.</param>
    /// <param name="timeTaken">The duration of the failed attempt in milliseconds.</param>
    /// <param name="crawledAt">The timestamp of the attempt.</param>
    /// <returns>A <see cref="CrawlResult"/> populated with error metadata and empty content.</returns>
    CrawlResult CreateErrorResult(
        string url, System.Net.HttpStatusCode statusCode, long timeTaken, DateTime crawledAt
    );
}