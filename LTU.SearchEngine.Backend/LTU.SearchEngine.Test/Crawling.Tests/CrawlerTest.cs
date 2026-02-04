using LTU.SearchEngine.Infrastructure.Crawling;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LTU.SearchEngine.Test.Crawling.Tests
{
    public class CrawlerTest
    {
        private readonly Mock<IHtmlParser> _parserMock;
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly Crawler _sut;

        public CrawlerTest()
        {
            _parserMock = new Mock<IHtmlParser>();
            _handlerMock = new Mock<HttpMessageHandler>();

            // We create a HttpClient that uses our mocked handler
            _httpClient = new HttpClient(_handlerMock.Object);

            _sut = new Crawler(_httpClient, _parserMock.Object);
        }

        [Fact]
        public async Task FetchAsync_WhenUrlIsValid_ReturnsSuccessfulCrawlResult()
        {
            // ARRANGE
            var url = "https://ltu.se";
            var fakeHtml = "<html><body>Hello</body></html>";
            SetupHttpResponse(HttpStatusCode.OK, fakeHtml);

            // ACT
            var result = await _sut.FetchAsync(url);

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(url, result.Url);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("sv", result.Language);                       
            Assert.Equal("Content not yet parsed", result.Words);
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
            Assert.Equal("", result.Words);
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
            Assert.Equal("", result.Words);                                     
        }

        [Fact]
        public async Task FetchAsync_WhenNetworkIsDown_ReturnsNull()
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
            Assert.Null(result);
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
                    Content = new StringContent(content)
                });
        }

    }
}
