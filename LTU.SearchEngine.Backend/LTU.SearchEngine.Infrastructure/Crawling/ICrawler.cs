using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

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

    /// <summary>
    /// Retrieves the content from the specified URL and generates a normalized SHA256 hash of its text.
    /// </summary>
    /// <remarks>
    /// This method fetches the raw data from the web, identifies the content type, and performs 
    /// specific extraction logic:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             For HTML: Extracts the main body text (removing scripts, styles, etc.) and normalizes 
    ///             whitespace and casing before hashing.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             For other formats: Currently defaults to hashing the extracted or raw text representation.
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <param name="url">The absolute URL of the page or file to be hashed.</param>
    /// <returns>
    /// A 64-character hexadecimal string representing the hash of the normalized content.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
    Task<string> GetContentHash(string url);
}