using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Crawling;
using LTU.SearchEngine.Test.HelperClasses;
using System.Diagnostics.Metrics;

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

    [Fact]
    public async Task ExtractTerms_Should_Clean_And_Rank_Complex_Html()
    {
        // ARRANGE
        // Load complex HTML containing CSS, JS, and various structure tags 
        // to verify robustness and ranking logic.
        var html = await _client.GetStringAsync("./TextExtractionQuality.html");

        var parser = new HapHtmlParser();

        // ACT
        var result = parser.ExtractTerms(html).ToList();

        // ASSERT

        // --- 1. CLEANUP VERIFICATION (FRQ-1007) ---
        // The parser must ignore non-relevant resources like CSS and JavaScript.
        // Assert that style definitions ('body { ... }') and script variables ('var x') are filtered out.
        Assert.DoesNotContain(result, t => t.Term.Contains("body"));
        Assert.DoesNotContain(result, t => t.Term.Contains("var"));

        // --- 2. TOKENIZATION VERIFICATION ---
        // Ensure the parser splits text into individual tokens (words) suitable for an Inverted Index.
        // We expect "Search" and "Engine" as separate tokens, not a single phrase "Search Engine...".
        Assert.Contains(result, t => t.Term == "Search");
        Assert.Contains(result, t => t.Term == "Engine");

        // --- 3. RANKING CONTEXT VERIFICATION (FRQ-3015) ---
        // Verify that terms are assigned the correct Source weight based on their HTML tag.
        // This metadata is critical for the Ranking Algorithm (TF/IDF + Boosting).

        // Case A: High Priority (Terms found in <title>)
        Assert.Contains(result, t => t.Term == "Extraction" && t.Source == TermSource.Title);

        // Case B: Medium Priority (Terms found in <h1> - <h6>)
        Assert.Contains(result, t => t.Term == "Result" && t.Source == TermSource.Header);

        // Case C: Standard Priority (Terms found in <body> paragraphs)
        Assert.Contains(result, t => t.Term == "primary" && t.Source == TermSource.Body);

        // --- 4. HYBRID STRATEGY VERIFICATION (Late Normalization) ---
        // Confirm that the parser preserves original casing (raw tokens).
        // Normalization (ToLower) is architecturally delegated to the Indexer component.
        Assert.Contains(result, t => t.Term == "Search");       // Expecting "Search"
        Assert.DoesNotContain(result, t => t.Term == "search"); // Not expecting "search" yet
    }

}
