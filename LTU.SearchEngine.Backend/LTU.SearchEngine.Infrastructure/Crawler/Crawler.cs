using LTU.SearchEngine.Backend.Core.Model;

namespace LTU.SearchEngine.Infrastructure.Crawler
{
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
            // 1. Hämta sidan (Infrastruktur)
            var response = await _httpClient.GetAsync(url);
            var htmlContent = await response.Content.ReadAsStringAsync();

            // 2. Använd den injicerade parsern för att hitta länkar och titel
            var links = _htmlParser.ExtractInternalLinks(htmlContent, url);
            var title = _htmlParser.ExtractTitle(htmlContent);

            // 3. Returnera resultatet (som vi definierat tidigare)
            return new CrawlResult(
            url,
    title,
    "sv", // Example Language
    "some words", // Example Words
    links,
    response.StatusCode,
    0 // Example TimeTakenMs
     );
        }
    }

}
