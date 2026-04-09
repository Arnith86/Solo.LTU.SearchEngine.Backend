using LTU.SearchEngine.Backend.Core.HelperClasses;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Crawling;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using Xunit.Abstractions;

namespace LTU.SearchEngine.Test.Crawling.Tests;

public class CrawlerTest
{
    private readonly Mock<IHtmlParser> _parserMock;
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly Mock<IContentHasher> _contentHasherMock;
    private readonly HttpClient _httpClient;
    private readonly Crawler _sut;
    private readonly ITestOutputHelper _output;

    public CrawlerTest(ITestOutputHelper output)
    {
        _parserMock = new Mock<IHtmlParser>();
        _handlerMock = new Mock<HttpMessageHandler>();
        _contentHasherMock = new Mock<IContentHasher>();
        _contentHasherMock.Setup(ch => ch.CalculateHash(It.IsAny<byte[]>())).Returns("FakeHash");

        // We create a HttpClient that uses our mocked handler
        _httpClient = new HttpClient(_handlerMock.Object);

        _sut = new Crawler(
            httpClient: _httpClient, 
            htmlParser: _parserMock.Object, 
            contentHasher: _contentHasherMock.Object,
            logger: new Mock<ILogger<Crawler>>().Object
            );
        _output = output;
    }


    [Fact]
    public async Task FetchAsync_WhenUrlIsValid_ReturnsSuccessfulCrawlResult()
    {
        // ARRANGE
        var url = "https://ltu.se";
        var fakeHtml = """
            <html lang="en">
                <head>
                  <title>Test Page Title</title>
                </head>
                <body>
                    <h1>Welcome to LTU</h1>
                    <p>This is a test page with search terms.</p>
                    <a href='/contact'>Contact Us</a>
                </body>
            </html>
            """;

        var expectedContent = System.Text.Encoding.UTF8.GetBytes(fakeHtml);

        // Create expected result
        var expectedTerms = new List<IndexedTerm> { new IndexedTerm("Welcome", TermSource.Header) };
        var expectedLinks = new List<string> { "https://ltu.se/contact" };
        var expectedTitle = "Test Page Title";
        var expectedHash = "TestHash";

        // Configure Moq
        _parserMock.Setup(p => p.ExtractTerms(It.IsAny<string>()))
            .Returns(expectedTerms);

        _parserMock.Setup(p => p.ExtractInternalLinks(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedLinks);

        _parserMock.Setup(p => p.ExtractTitle(It.IsAny<string>()))
            .Returns(expectedTitle);
    
        _parserMock.Setup(p => p.ExtractLanguage(It.IsAny<string>()))
            .Returns("en");
        
        HttpResponseHelper.SetupHttpResponse(_handlerMock, HttpStatusCode.OK, fakeHtml);

        var rawCrawlData = RawCrawlDataBuilder.BuildRawCrawlData(
            url: url,
            content: expectedContent,
            timeTaken: 1000
        );

        // ACT
        var result = await _sut.FetchAsync(rawCrawlData, expectedHash);


        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(url, result.Url);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("en", result.Language);
        Assert.NotEmpty(result.IndexedTerms);
        Assert.Equal(expectedContent, result.Content);
        Assert.True(result.TimeTakenMs >= 0);
    }


    [Fact]
    public async Task FetchRawAsync_WhenUrlIsValid_ReturnsRawDataWithMetadata()
    {
        // Arrange
        var url = "https://ltu.se";
        var statusCode = HttpStatusCode.OK;
        var contentType = "text/html";
        var charSet = "utf-8";
        var fakeHtml = """<html lang="en"></html>""";
        byte[] encodedContent = Encoding.UTF8.GetBytes(fakeHtml);
        
        HttpResponseHelper.SetupHttpResponse(
            httpMessageHandlerMock: _handlerMock, 
            httpStatusCode: statusCode, 
            content: fakeHtml,
            contentType: contentType,
            charSet: charSet
        );

        // Act 
        var result = await _sut.FetchRawAsync(url);

        // Assert 
        Assert.NotNull(result);
        Assert.Equal(statusCode, result.HttpStatusCode);
        Assert.Equal(encodedContent, result.Content);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(charSet, result.CharSet);
    }

    [Fact]
    public async Task GetContentHash_WhenHtml_InvokesParserAndHasher()
    {
        // Arrange
        string fakeHash = "FakeHash";

        var rawData = RawCrawlDataBuilder.BuildRawCrawlData(
            url: "https://ltu.se",
            content: Encoding.UTF8.GetBytes("<html></html>"),
            contentType: "text/html",
            timeTaken: 400
        );
 
        _parserMock.Setup(p => p.ExtractRawText(It.IsAny<string>())).Returns("clean text");
        _parserMock.Setup(p => p.CleanRawTextForHashing(It.IsAny<string>())).Returns("clean text");
        _contentHasherMock.Setup(c => c.CalculateHash(It.IsAny<string>())).Returns(fakeHash);

        // Act 
        var result = await _sut.GetContentHash(rawData);

        // Assert    
        Assert.Equal(fakeHash, result);
        _parserMock.Verify(p => p.ExtractRawText(It.IsAny<string>()), Times.Once);
        _parserMock.Verify(p => p.CleanRawTextForHashing(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void CreateErrorResult_ReturnsPopulatedErrorObject()
    {
        // Arrange
        var url = "https://ltu.se";
        var time = 100L;
        var now = DateTime.UtcNow;

        // Act
        var result = _sut.CreateErrorResult(url, HttpStatusCode.NotFound, time, now);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal(url, result.Url);
        Assert.Empty(result.IndexedTerms);
        Assert.Equal("Unknown", result.Language);
    }
}
