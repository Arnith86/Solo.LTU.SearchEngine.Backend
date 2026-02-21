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
        public void IsAllowed_WhenUrlIsDisallowed_ReturnFalse()
        {
            //Arrange

            //Setting up a lturobots.txt with rules we can test agaíns and adding a wildcard in the end (*.pdf) to test our wildcard-logic
            string ltuRobots = "User-agent: TestCrawler\nDisallow: /student/\nDisallow: /*.pdf$";
            SetupRobotsTxtResponse(ltuRobots);
            var handler = new RobotsHandler(_httpClient, _settings);

            //Act
            bool result = handler.IsAllowed(("https://www.ltu.se/student/schema"));

            //Assert
            Assert.False(result, "URLshould be blocked according to LTUs mocked robots.txt");
        }



       
    }
}
