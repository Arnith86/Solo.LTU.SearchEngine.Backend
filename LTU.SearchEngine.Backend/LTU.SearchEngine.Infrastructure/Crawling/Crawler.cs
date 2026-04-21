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
    
    
    /// <inheritdoc/>
    public async Task<RawCrawlData> FetchRawAsync(string url)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.GetAsync(url);
        stopwatch.Stop();

        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType;

        return new RawCrawlData(
            Url: url, 
            TimeTaken: stopwatch.ElapsedMilliseconds,
            HttpStatusCode: response.StatusCode,
            Content: bytes, 
            ContentType: contentType?.MediaType ?? "text/plain", 
            CharSet: contentType?.CharSet
        );
    }


    /// <inheritdoc/>
    public async Task<string> GetContentHash(RawCrawlData data)
    {
        string hashableText = string.Empty;

        if (IsHtmlFormat(data.ContentType))
        {
            string html = GetHtmlString(
                data,
                SetEncoding(data.CharSet)
            );

            hashableText = _htmlParser.CleanRawTextForHashing(
                _htmlParser.ExtractRawText(html)
            );    
        }
        
        return _contentHasher.CalculateHash(hashableText);
    }


    /// <inheritdoc/>
    public async Task<CrawlResult> FetchAsync(RawCrawlData data, string hashedContent)
    {
        var crawledAt = DateTime.UtcNow;

        var terms = Enumerable.Empty<IndexedTerm>();
        var links = Enumerable.Empty<string>();
        string title = null!;
        string languageCode = "Unknown";
        DocumentMetaData metaData = null!;

        if (IsHtmlFormat(data.ContentType))
        {
            // If charset was missing or unrecognized, default to UTF-8
            var htmlString = GetHtmlString(
                data, 
                SetEncoding(data.CharSet)
            );

            languageCode = _htmlParser.ExtractLanguage(htmlString);
            metaData = _htmlParser.ExtractHtmlMetaData(htmlString);
            title = _htmlParser.ExtractTitle(htmlString);
            links = await _htmlParser.ExtractInternalLinks(htmlString, data.Url);
            terms = _htmlParser.ExtractTerms(htmlString);
        }

        return new CrawlResult(
            url: data.Url,
            title: title,
            language: languageCode,
            indexedTerms: terms,
            type: data.ContentType,
            metaData: metaData,
            content: data.Content,
            extractedLinks: links,
            statusCode: data.HttpStatusCode,
            timeTakenMs: data.TimeTaken,
            contentHash: hashedContent,
            crawledAt: crawledAt
        );
    }



    /// <inheritdoc/>
    public CrawlResult CreateErrorResult(
        string url, System.Net.HttpStatusCode statusCode, long timeTaken, DateTime crawledAt)
    {
        return new CrawlResult(
            url: url,
            title: null,
            language: "Unknown",
            indexedTerms: Enumerable.Empty<IndexedTerm>(),
            type: "Unknown",
            metaData: new HtmlDocumentMetaData("Unknown", "Unknown"),
            content: Array.Empty<byte>(),
            extractedLinks: Enumerable.Empty<string>(),
            statusCode: statusCode,
            timeTakenMs: timeTaken,
            contentHash: "noHash",
            crawledAt: crawledAt
        );
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
    
   
    private string GetHtmlString(RawCrawlData data, Encoding encoding)
    {
        // // If charset was missing or unrecognized, default to UTF-8
        // var encoding = SetEncoding(data.CharSet);
    
        return encoding.GetString(data.Content);
    }
}
