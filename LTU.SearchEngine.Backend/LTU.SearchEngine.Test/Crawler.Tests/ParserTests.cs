using LTU.SearchEngine.Infrastructure;

namespace LTU.SearchEngine.Test.Crawler.Tests;

public class ParserTests
{
	private readonly HttpClient _client;

	public ParserTests()
	{
		WebHostBuilder wBuilder = new WebHostBuilder();
		_client = wBuilder.BuildHttpClient();
	}

	[Fact]
	public async Task HapHtmlParser_ExtractInternalLinks_FindsAllLinksInDocument()
	{
		// Arrange
		var parser = new HapHtmlParser();
		var html = await _client.GetStringAsync("/LinksAndPDFDetection.html");

		// Act
		var links = parser.ExtractInternalLinks(
			html,
			"http://localhost/LinksAndPDFDetection.html");

		// Assert
		Assert.Contains(
			"http://localhost/Linked.html",
			links);
		Assert.Contains(
			"http://localhost/HelloWorld.pdf",
			links);
        Assert.Contains(
			new Uri("https://google.com").AbsoluteUri,
		    links);

    }
}
