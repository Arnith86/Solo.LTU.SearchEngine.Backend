using LTU.SearchEngine.Infrastructure;

namespace LTU.SearchEngine.Test.Crawling.Tests;

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

	[Fact]
	public async Task ExtractTitle_ShouldReturnTitle_WhenTagExist()
	{
		//Arrange
		var parser = new HapHtmlParser();
		var html = await _client.GetStringAsync("./LinksAndPDFDetection.html");

		//Act
		var title = parser.ExtractTitle(html);

		//Assert
		Assert.Contains("Student & Forskning är viktigt", title);
	}

	[Fact]
	public async Task ExtractTitle_ShouldDecodeHtmlEntities()
	{
        // Arrange
        var parser = new HapHtmlParser();
        string html = await _client.GetStringAsync("./LinksAndPDFDetection.html");

        // Act
        string result = parser.ExtractTitle(html);

        // Assert
        // Vi förväntar oss att &amp; blir "&" och &#228; blir "ä"
        Assert.Equal("Student & Forskning är viktigt", result);
    }

	[Fact]
	public async Task ExtractTitle_ShouldTrimWhiteSpaces()
	{
		//Arrange
		var parser = new HapHtmlParser();
		string html = await _client.GetStringAsync("./TextExtractionQuality.html");

		//Act
		string result = parser.ExtractTitle(html);

		//Assert
		Assert.Equal("Test: Text Extraction Quality", result);
	}

	[Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractTitle_ShouldReturnEmpty_WhenInputIsInvalid(string badHtml)
	{
        //Arrange
        var parser = new HapHtmlParser();

        // Act
        string result = parser.ExtractTitle(badHtml);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
