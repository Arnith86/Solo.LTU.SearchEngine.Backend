using LTU.SearchEngine.Backend.Core.HelperClasses;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Diagnostics;

namespace LTU.SearchEngine.Infrastructure.Crawling;

public class Crawler : ICrawler
{
    private readonly HttpClient _httpClient;
    private readonly IHtmlParser _htmlParser;
    private readonly IContentHasher _contentHasher;

    public Crawler(HttpClient httpClient, IHtmlParser htmlParser, IContentHasher contentHasher)
    {
        _httpClient = httpClient;
        _htmlParser = htmlParser;
        _contentHasher = contentHasher;
    }

    /// <inheritdoc/>
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
                return CreateErrorResult(url, response.StatusCode, stopwatch.ElapsedMilliseconds, "None");
            }

            //if call successful get the data
            byte[] content = await response.Content.ReadAsByteArrayAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";

            var terms = Enumerable.Empty<IndexedTerm>();
            var links = Enumerable.Empty<string>();
            string title = null!;

            if (contentType.Contains("text/html"))
            {
                var htmlString = System.Text.Encoding.UTF8.GetString(content);

                terms = _htmlParser.ExtractTerms(htmlString);
                links = _htmlParser.ExtractInternalLinks(htmlString, url);
                title = _htmlParser.ExtractTitle(htmlString);
            }

            return new CrawlResult(
                url: url,
                title: title,
                language: "Unknown",
                indexedTerms: terms,
                type: contentType,
                content: content,
                extractedLinks: links,
                statusCode: response.StatusCode,
                timeTakenMs: stopwatch.ElapsedMilliseconds,
                contentHash: _contentHasher.CalculateHash(content)
            );
        }
        catch (HttpRequestException ex) 
        {
            stopwatch.Stop();
            return CreateErrorResult(url, System.Net.HttpStatusCode.ServiceUnavailable, stopwatch.ElapsedMilliseconds, $"Error: {ex}");
        }
        catch (Exception) 
        {
            stopwatch.Stop();
            return null!; 
        }
    }

    // Help method for creating a "failed" result
    private CrawlResult CreateErrorResult(string url, System.Net.HttpStatusCode statusCode, long timeTaken, string type)
    {
        return new CrawlResult(
            url: url,
            title: null,
            language: "Unknown",
            indexedTerms: Enumerable.Empty<IndexedTerm>(),
            type: type,
            content: Array.Empty<byte>(),
            extractedLinks: Enumerable.Empty<string>(),
            statusCode: statusCode,
            timeTakenMs: timeTaken,
            contentHash: _contentHasher.CalculateHash(Array.Empty<byte>())
        );
    }
}
