using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
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
                    url: url,
                    title: null,
                    language: "Unknown",
                    indexedTerms: Enumerable.Empty<IndexedTerm>(),
                    type: "None",
                   content: Array.Empty<byte>(),                
                extractedLinks: Enumerable.Empty<string>(),
                statusCode: response.StatusCode,
                timeTakenMs: stopwatch.ElapsedMilliseconds
                );
            }

            //if call successful get the data
            byte[] content = await response.Content.ReadAsByteArrayAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";

           
            return new CrawlResult(
      url: url,
            title: null,
            language: "sv",
            indexedTerms: Enumerable.Empty<IndexedTerm>(),
            type: contentType,
            content: content,
            extractedLinks: Enumerable.Empty<string>(),
            statusCode: response.StatusCode,
            timeTakenMs: stopwatch.ElapsedMilliseconds
            );
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();
            return null;
        }
    }
}
