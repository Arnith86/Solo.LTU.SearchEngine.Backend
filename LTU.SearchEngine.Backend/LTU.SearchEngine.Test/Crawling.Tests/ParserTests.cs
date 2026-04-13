using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Configurations;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.Extensions.Logging;
using Moq;

namespace LTU.SearchEngine.Test.Crawling.Tests;

public class ParserTests
{
    private readonly HapHtmlParser _sut;
	private readonly HttpClient _client;
    private readonly Mock<IDomainValidator> _domainValidatorMock;
    private readonly Mock<IRobotsHandler> _robotsHandlerMock;
    
	public ParserTests()
	{
        _domainValidatorMock = new Mock<IDomainValidator>();
        _robotsHandlerMock = new Mock<IRobotsHandler>();
        
		WebHostBuilder wBuilder = new WebHostBuilder();
		_client = wBuilder.BuildHttpClient();
        _sut = new HapHtmlParser(
            _domainValidatorMock.Object,
            _robotsHandlerMock.Object,
            new Mock<ILogger<HapHtmlParser>>().Object
        );
	}

	[Fact]
	public async Task HapHtmlParser_ExtractInternalLinks_FindsAllLinksInDocument()
	{
		// Arrange
		var html = await _client.GetStringAsync("/LinksAndPDFDetection.html");
        
        _domainValidatorMock.Setup(dv => dv.IsWhitelisted(It.IsAny<string>())).Returns(true);
        _robotsHandlerMock.Setup(rh => rh.IsAllowedAsync(It.IsAny<string>())).ReturnsAsync(true);

		// Act
		var links = await _sut.ExtractInternalLinks(
			html,
			"http://localhost/LinksAndPDFDetection.html"
        );

		// Assert
		Assert.Contains("http://localhost/Linked.html", links);
		Assert.Contains("http://localhost/HelloWorld.pdf", links);
        Assert.Contains(new Uri("https://google.com").AbsoluteUri, links);

    }

   
    [Fact]
	public async Task ExtractInternalLinks_WhenNotInWhiteList_ShouldNotBeFoundInLinks()
	{
        // Arrange
        var html = await _client.GetStringAsync("/LinksAndPDFDetection.html");

		// Arrange
		_domainValidatorMock
			.Setup(v => v.IsWhitelisted(It.IsAny<string>()))
			.Returns(true);

		_robotsHandlerMock
			.Setup(rh => rh.IsAllowedAsync(It.IsAny<string>()))
			.ReturnsAsync(false);

		// Act & Assert
		var links = await _sut.ExtractInternalLinks(html, "http://localhost/LinksAndPDFDetection.html");
		
        Assert.DoesNotContain("http://localhost/Linked.html", links);
        Assert.DoesNotContain("http://localhost/HelloWorld.html", links);
	}
   
   
    [Fact]
	public async Task ExtractInternalLinks_WhenBlockedByRobotsTxt_ShouldNotBeFoundInLinks()
	{
        // Arrange
        var html = await _client.GetStringAsync("/LinksAndPDFDetection.html");

		// Arrange
		_domainValidatorMock
			.Setup(v => v.IsWhitelisted(It.IsAny<string>()))
			.Returns(false);

		_robotsHandlerMock
			.Setup(rh => rh.IsAllowedAsync(It.IsAny<string>()))
			.ReturnsAsync(true);

		// Act & Assert
		var links = await _sut.ExtractInternalLinks(html, "http://localhost/LinksAndPDFDetection.html");
		
        Assert.DoesNotContain("http://localhost/Linked.html", links);
        Assert.DoesNotContain("http://localhost/HelloWorld.html", links);
	}



	[Fact]
	public async Task ExtractTitle_ShouldReturnTitle_WhenTagExist()
	{
		//Arrange
		var html = await _client.GetStringAsync("./LinksAndPDFDetection.html");

		//Act
		var title = _sut.ExtractTitle(html);

		//Assert
		Assert.Contains("Student & Forskning är viktigt", title);
	}

	[Fact]
	public async Task ExtractTitle_ShouldDecodeHtmlEntities()
	{
        // Arrange
        string html = await _client.GetStringAsync("./LinksAndPDFDetection.html");

        // Act
        string result = _sut.ExtractTitle(html);

        // Assert
        // Vi förväntar oss att &amp; blir "&" och &#228; blir "ä"
        Assert.Equal("Student & Forskning är viktigt", result);
    }

	[Fact]
	public async Task ExtractTitle_ShouldTrimWhiteSpaces()
	{
		//Arrange
		string html = await _client.GetStringAsync("./TextExtractionQuality.html");

		//Act
		string result = _sut.ExtractTitle(html);

		//Assert
		Assert.Equal("Test: Text Extraction Quality", result);
	}

	[Theory]
    [InlineData("NULL_TEST")]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractTitle_ShouldReturnEmpty_WhenInputIsInvalid(string input)
	{
        // Arrange
        string? badHtml = input.Equals("NULL_TEST") ? null : input;
        
        // Act
        string result = _sut.ExtractTitle(badHtml!);

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
        
        // ACT
        var result = _sut.ExtractTerms(html).ToList();

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

        //test for image
        Assert.Contains(result, t => t.Term == "LTU" && t.Source == TermSource.Body);
        Assert.Contains(result, t => t.Term == "Logo" && t.Source == TermSource.Body);

        Assert.Contains(result, t => t.Term == "information" && t.Source == TermSource.Body);

        // Case C: Standard Priority (Terms found in <body> paragraphs)
        Assert.Contains(result, t => t.Term == "primary" && t.Source == TermSource.Body);



        // --- 4. HYBRID STRATEGY VERIFICATION (Late Normalization) ---
        // Confirm that the parser preserves original casing (raw tokens).
        // Normalization (ToLower) is architecturally delegated to the Indexer component.
        Assert.Contains(result, t => t.Term == "Search");       // Expecting "Search"
        Assert.DoesNotContain(result, t => t.Term == "search"); // Not expecting "search" yet
    }

    [Fact]
    public async Task ExtractRawText_ShouldReturnCleanText_IgnoringCodeTags()
    {
        // ARRANGE
        var html = await _client.GetStringAsync("./TextExtractionQuality.html");
        
        // ACT
        var result = _sut.ExtractRawText(html);

        // ASSERT
        Assert.DoesNotContain("body {", result); 
        Assert.DoesNotContain("var x =", result); 
        Assert.Contains("Search Engine", result); 
    }

    [Fact]
    public void ExtractRawText_ShouldDecodeEntitiesAndTrim()
    {
        // ARRANGE
        var html = "<div> &nbsp; Student &amp; Forskning &nbsp; </div>";

        // ACT
        var result = _sut.ExtractRawText(html);

        // ASSERT
        Assert.Equal("Student & Forskning", result);
    }

    [Theory]
    [InlineData("NULL_TEST")]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractRawText_ShouldReturnEmpty_WhenInputIsInvalid(string input)
    {
        // ARRANGE
        string? badHtml = input.Equals("NULL_TEST") ? null : input;

        // ACT
        var result = _sut.ExtractRawText(badHtml!);

        // ASSERT
        Assert.Equal(string.Empty, result);
    }
}
