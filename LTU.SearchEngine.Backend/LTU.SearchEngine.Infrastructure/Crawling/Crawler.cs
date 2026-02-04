using LTU.SearchEngine.Backend.Core.Model;
using System.Diagnostics;

namespace LTU.SearchEngine.Infrastructure.Crawling;

public class Crawler : ICrawler
{
    private readonly HttpClient _httpClient;
    private readonly IHtmlParser _htmlParser;

    public Crawler(HttpClient httpClient, IHtmlParser htmlParser)
    {
        _httpClient = httpClient;
        _htmlParser = htmlParser;
    }

    /// <summary>
    /// Asynchronously fetches content from the specified URL, measures the request duration, 
    /// and handles HTTP error codes such as 404 or 500.
    /// </summary>
    /// <param name="url">The web address to fetch.</param>
    /// <returns>
    /// A <see cref="CrawlResult"/> containing the status code, elapsed time, and raw data; 
    /// or <c>null</c> if a critical network error occurs.
    /// </returns>
    public async Task<CrawlResult> FetchAsync(string url)
    {
        var stopwatch = Stopwatch.StartNew(); 

        try
        {
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();

            // Handle 404/500 errors per Acceptance Criteria: 
            // Return status code without body content.
            if (!response.IsSuccessStatusCode)
            {
                return new CrawlResult(
                    url,
                    null,                // Title
                    "Unknown",           // Language
                    "",                  // Words
                    new List<string>(),  // ExtractedLinks
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                );
            }

            // Currently only fetching data; parsing for Language and Words is not yet implemented.
            return new CrawlResult(
                url,
                null,                       // Title (fylls i senare av parsern)
                "sv",                       // Language (placeholder)
                "Content not yet parsed",    // Words (placeholder)
                new List<string>(),         // ExtractedLinks (fylls i senare)
                response.StatusCode,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();
            return null;
        }
    }
}
