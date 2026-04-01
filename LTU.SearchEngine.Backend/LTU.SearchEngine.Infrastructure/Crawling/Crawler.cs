
using System.Diagnostics;
using System.Text;
using LTU.SearchEngine.Backend.Core.HelperClasses;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LTU.SearchEngine.Infrastructure.Crawling;

public class Crawler : ICrawler
{
    private readonly HttpClient _httpClient;
    private readonly IHtmlParser _htmlParser;
    private readonly IContentHasher _contentHasher;
    private readonly ILogger<Crawler> _logger;

    public Crawler(HttpClient httpClient, IHtmlParser htmlParser, IContentHasher contentHasher, ILogger<Crawler> logger)
    {
        _httpClient = httpClient;
        _htmlParser = htmlParser;
        _contentHasher = contentHasher;
        _logger = logger;
    }


    private bool IsHtmlFormat(string format) => format.Contains("text/html");

    private Encoding SetEncoding(string? charSet)
    {
        var encoding = Encoding.UTF8; 

        if (!string.IsNullOrWhiteSpace(charSet))
        {
            try
            {
                encoding = Encoding.GetEncoding(charSet);
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Failed to get encoding for charset: {CharSet}, defaulting to UTF-8.", charSet);
            }
        }
        return encoding;
    }

    private string GetHtmlString(byte[] content, HttpResponseMessage response)
    {
        // If charset was missing or unrecognized, default to UTF-8
        string? charSet = response.Content.Headers.ContentType?.CharSet; 
        var encoding = SetEncoding(charSet);
    
        return encoding.GetString(content);
    }

    /// <inheritdoc/>
    public async Task<CrawlResult> FetchAsync(string url)
    {
        var stopwatch = Stopwatch.StartNew();
        var crawledAt = DateTime.UtcNow;

        try
        {
            using var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();

            // Handle 404/500 errors per Acceptance Criteria: 
            // Return status code without body content.
            if (!response.IsSuccessStatusCode)
                return CreateErrorResult(url, response.StatusCode, stopwatch.ElapsedMilliseconds, "None", crawledAt);
            

            //if call successful get the data
            byte[] content = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType;

            string? format = contentType?.MediaType ?? "text/plain";

            var terms = Enumerable.Empty<IndexedTerm>();
            var links = Enumerable.Empty<string>();
            string title = null!;
            string languageCode = "Unknown";

            if (IsHtmlFormat(format))
            {
                var htmlString = GetHtmlString(content, response);

                languageCode = _htmlParser.ExtractLanguage(htmlString);
                title = _htmlParser.ExtractTitle(htmlString);
                links = await _htmlParser.ExtractInternalLinks(htmlString, url);
                terms = _htmlParser.ExtractTerms(htmlString);
            }

            return new CrawlResult(
                url: url,
                title: title,
                language: languageCode,
                indexedTerms: terms,
                type: format,
                content: content,
                extractedLinks: links,
                statusCode: response.StatusCode,
                timeTakenMs: stopwatch.ElapsedMilliseconds,
                contentHash: _contentHasher.CalculateHash(content),
                crawledAt: crawledAt
            );
        }
        catch (HttpRequestException ex) 
        {
            stopwatch.Stop();
            return CreateErrorResult(
                url, 
                System.Net.HttpStatusCode.ServiceUnavailable,
                stopwatch.ElapsedMilliseconds, 
                $"Error: {ex}",
                crawledAt
            );
        }
        catch (Exception e) 
        {   Debug.WriteLine(e);
            stopwatch.Stop();
            return null!; 
        }
    }

    // Help method for creating a "failed" result
    private CrawlResult CreateErrorResult(
        string url, System.Net.HttpStatusCode statusCode, long timeTaken, string type, DateTime crawledAt)
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
            contentHash: _contentHasher.CalculateHash(Array.Empty<byte>()), 
            crawledAt: crawledAt
        );
    }
}
