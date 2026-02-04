using LTU.SearchEngine.Backend.Core.Model;

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

    public async Task<CrawlResult> FetchAsync(string url)
    {
        throw new NotImplementedException("Logiken för FetchAsync implementeras i issue [30]");

    }
}
