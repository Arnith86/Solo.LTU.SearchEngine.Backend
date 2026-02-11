using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Crawling;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit.Abstractions;

namespace LTU.SearchEngine.Test.Crawling.Tests
{
    public class CrawlerTest
    {
        private readonly Mock<IHtmlParser> _parserMock;
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly Crawler _sut;
        private readonly ITestOutputHelper _output;

        public CrawlerTest(ITestOutputHelper output)
        {
            _parserMock = new Mock<IHtmlParser>();
            _handlerMock = new Mock<HttpMessageHandler>();

            // We create a HttpClient that uses our mocked handler
            _httpClient = new HttpClient(_handlerMock.Object);

            _sut = new Crawler(_httpClient, _parserMock.Object);
            _output = output;
        }


        [Fact]
        public async Task FetchAsync_WhenUrlIsValid_ReturnsSuccessfulCrawlResult()
        {
            // ARRANGE
            var url = "https://ltu.se";
            var fakeHtml = @"
            <html>
                <head>
                  <title>Test Page Title</title>
          </head>
          <body>
        <h1>Welcome to LTU</h1>
        <p>This is a test page with search terms.</p>
        <a href='/contact'>Contact Us</a>
    </body>
        </html>";

            var expectedContent = System.Text.Encoding.UTF8.GetBytes(fakeHtml);

            // Create expected result
            var expectedTerms = new List<IndexedTerm> { new IndexedTerm("Welcome", TermSource.Header) };
            var expectedLinks = new List<string> { "https://ltu.se/contact" };
            var expectedTitle = "Test Page Title";

            // Configure Moq
            _parserMock.Setup(p => p.ExtractTerms(It.IsAny<string>()))
                .Returns(expectedTerms);

            _parserMock.Setup(p => p.ExtractInternalLinks(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(expectedLinks);

            _parserMock.Setup(p => p.ExtractTitle(It.IsAny<string>()))
                .Returns(expectedTitle);

            SetupHttpResponse(HttpStatusCode.OK, fakeHtml);

            // ACT
            var result = await _sut.FetchAsync(url);


            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(url, result.Url);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("sv", result.Language);
            Assert.NotEmpty(result.IndexedTerms);
            Assert.Equal(expectedContent, result.Content);
            Assert.True(result.TimeTakenMs >= 0);
        }

        [Fact]
        public async Task FetchAsync_WhenPageIsNotFound_ReturnsResultWith404Status()
        {
            // ARRANGE
            var url = "https://ltu.se/finns-inte";
            SetupHttpResponse(HttpStatusCode.NotFound, "");

            // ACT
            var result = await _sut.FetchAsync(url);

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Null(result.Title);
            Assert.Equal("Unknown", result.Language);
            Assert.Empty(result.IndexedTerms);
            Assert.Empty(result.ExtractedLinks);

            Assert.Empty(result.Content);

            Assert.True(result.TimeTakenMs >= 0);
        }

        [Fact]
        public async Task FetchAsync_WhenServerErrorOccurs_ReturnsResultWith500Status()
        {
            // ARRANGE
            var url = "https://ltu.se/trasig-sida";

            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

            // ACT
            var result = await _sut.FetchAsync(url);

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.Equal("Unknown", result.Language);
            Assert.Empty(result.IndexedTerms);

            Assert.Empty(result.Content);

            Assert.True(result.TimeTakenMs >= 0);
        }

        [Fact]
        public async Task FetchAsync_WhenNetworkIsDown_ReturnsErrorResult()
        {
            // ARRANGE
            var url = "https://ltu.se";

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("No internet connection"));

            // ACT
            var result = await _sut.FetchAsync(url);

            // ASSERT
            Assert.NotNull(result); 
            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, result.StatusCode);
            Assert.Equal("Error", result.Type);
            Assert.Equal(url, result.Url);
            Assert.Empty(result.IndexedTerms);
        }

        // --- Helpmethod for moq HttpClient ---
        private void SetupHttpResponse(HttpStatusCode code, string content)
        {
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = code,
                    Content = new StringContent(content, System.Text.Encoding.UTF8, "text/html")
                });
        }
    }
}
