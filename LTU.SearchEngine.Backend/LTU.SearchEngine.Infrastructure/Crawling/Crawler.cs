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

            var terms = Enumerable.Empty<IndexedTerm>();
            var links = Enumerable.Empty<string>();
            string title = null;

            if (contentType.Contains("text/html"))
            {
                var htmlString = System.Text.Encoding.UTF8.GetString(content);

                // HÄR: Använd ExtractTerms (plural) som vi definierade i Issue 1
                terms = _htmlParser.ExtractTerms(htmlString);
                links = _htmlParser.ExtractInternalLinks(htmlString, url);
                title = _htmlParser.ExtractTitle(htmlString);
            }

            return new CrawlResult(
            url: url,
            title: title,
            language: "sv",
           indexedTerms: terms,
            type: contentType,
            content: content,
            extractedLinks: links,
            statusCode: response.StatusCode,
            timeTakenMs: stopwatch.ElapsedMilliseconds
            );
        }
        catch (HttpRequestException ex) // Fånga nätverksfel
        {
            stopwatch.Stop();
            // Returnera ett "misslyckat" resultat istället för null, så pipelinen inte kraschar
            return new CrawlResult(
                url: url,
                title: null,
                language: "Unknown",
                indexedTerms: Enumerable.Empty<IndexedTerm>(),
                type: "Error",
                content: Array.Empty<byte>(),
                extractedLinks: Enumerable.Empty<string>(),
                statusCode: System.Net.HttpStatusCode.ServiceUnavailable, // Eller liknande
                timeTakenMs: stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception) // Fånga allt annat oväntat
        {
            stopwatch.Stop();
            return null; // I värsta fall, men helst returnera ett Error-resultat här med.
        }
    }
}
