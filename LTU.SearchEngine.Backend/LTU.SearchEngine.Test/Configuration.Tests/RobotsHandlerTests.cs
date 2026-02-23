using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Configurations;
using Moq;
using Moq.Protected;
using System.Net;

namespace LTU.SearchEngine.Test.Configuration.Tests
{
    public class RobotsHandlerTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly CrawlerSettings _settings;

        public RobotsHandlerTests()
        {
            //setting up a mocked handler to simulate networkcalls
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

            _settings = new CrawlerSettings(
                userAgent: "TestCrawler",
                maxConcurrencyPerDomain: 5,
                minDelayMs: 0,
                retryIntervals: new[] { TimeSpan.FromSeconds(1) },
                seedUrls: new[] { "https://ltu.se" }
            );
        }

        //Helpmethod for setting up our faked robots.txt
        private void SetupRobotsTxtResponse(string content)
        {
            _httpMessageHandlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(content)
               });
        }

        [Fact]
        public void IsAllowed_WhenUrlIsDisallowed_ReturnsFalse()
        {
            //Arrange

            //Setting up a lturobots.txt with rules we can test agains and adding a wildcard in the end (*.pdf) to test our wildcard-logic
            string ltuRobots = "User-agent: TestCrawler\nDisallow: /student/\nDisallow: /*.pdf$";
            SetupRobotsTxtResponse(ltuRobots);
            var handler = new RobotsHandler(_httpClient, _settings);

            //Act
            bool result = handler.IsAllowed(("https://www.ltu.se/student/schema"));

            //Assert
            Assert.False(result, "URLshould be blocked according to LTUs mocked robots.txt");
        }

        [Fact]
        public void IsAllowed_WhenUrlIsAllowed_ReturnsTrue()
        {
            //Arrange
            string ltuRobots = "User-agent: TestCrawler\nDisallow: /student/";
            SetupRobotsTxtResponse(ltuRobots);
            var handler = new RobotsHandler(_httpClient, _settings);

            //Act
            bool result = handler.IsAllowed("https://www.ltu.se/utbildning/program");

            //Assert
            Assert.True(result, "URL should be allowed because it is not effected of disallow-rules");
        }

        [Fact]
        public void IsAllowed_FetchesRobotsTxt_ExactlyOncePerDomain()
        {
            // Arrange
            string ltuRobots = "User-agent: *\nDisallow: /admin/";
            SetupRobotsTxtResponse(ltuRobots);
            var handler = new RobotsHandler(_httpClient, _settings);

            //Act: We asks the crawler to evaluate 3 different urls on LTU
            handler.IsAllowed("https://www.ltu.se/forskning");
            handler.IsAllowed("https://www.ltu.se/om-ltu");
            handler.IsAllowed("https://www.ltu.se/admin/login");


            //Assert: Contols that the HTTP-klient ONLY did one call to LTUs domain
            _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsoluteUri == "https://www.ltu.se/robots.txt"),
            ItExpr.IsAny<CancellationToken>()
        );
        }
    }
}
