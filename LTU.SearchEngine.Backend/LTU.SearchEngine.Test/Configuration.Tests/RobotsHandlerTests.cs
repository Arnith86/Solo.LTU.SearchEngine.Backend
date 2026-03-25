using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Configuration;
using LTU.SearchEngine.Infrastructure.Configurations;
using Moq;
using Moq.Protected;
using System.Net;

namespace LTU.SearchEngine.Test.Configuration.Tests;

public class RobotsHandlerTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly CrawlerSettings _settings;
    private readonly Mock<ICrawlerSettingsLoader> _settingsLoaderMock;

    public RobotsHandlerTests()
    {
        // setting up a mocked handler to simulate network calls
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _settingsLoaderMock = new Mock<ICrawlerSettingsLoader>();

        _settings = new CrawlerSettings(
            userAgent: "TestCrawler",
            maxConcurrencyPerDomain: 5,
            minDelayMs: 0,
            retryIntervals: new[] { TimeSpan.FromSeconds(1) },
            seedUrls: new[] { "https://ltu.se" },
            whiteList: new List<string> { "ltu.se" },
            robotsExceptionRules: new Dictionary<string, List<string>>{
                { "ltu.se", new List<string> { "/private/" } }
            }
        );
        
        _settingsLoaderMock.Setup(sl => sl.Load()).Returns(_settings);
    }

    // Help method for setting up our faked robots.txt
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
    public async Task IsAllowed_WhenUrlIsDisallowed_ReturnsFalse()
    {
        // Arrange

        // Setting up a lturobots.txt with rules we can test again and adding a wildcard in the end (*.pdf) to test our wildcard-logic
        string ltuRobots = "User-agent: TestCrawler\nDisallow: /student/\nDisallow: /*.pdf$";
        SetupRobotsTxtResponse(ltuRobots);
        var handler = new RobotsHandler(_httpClient, _settingsLoaderMock.Object);

        // Act
        bool result = await handler.IsAllowedAsync(("https://www.ltu.se/student/schema"));

        // Assert
        Assert.False(result, "URLshould be blocked according to LTUs mocked robots.txt");
    }


    [Fact]
    public async Task IsAllowed_WhenUrlIsAllowed_ReturnsTrue()
    {
        //Arrange
        string ltuRobots = "User-agent: TestCrawler\nDisallow: /student/";
        SetupRobotsTxtResponse(ltuRobots);
        var handler = new RobotsHandler(_httpClient, _settingsLoaderMock.Object);

        //Act
        bool result = await handler.IsAllowedAsync("https://www.ltu.se/utbildning/program");

        //Assert
        Assert.True(result, "URL should be allowed because it is not effected of disallow-rules");
    }


    [Fact]
    public async Task IsAllowed_FetchesRobotsTxt_ExactlyOncePerDomain()
    {
        // Arrange
        string ltuRobots = "User-agent: *\nDisallow: /admin/";
        SetupRobotsTxtResponse(ltuRobots);
        var handler = new RobotsHandler(_httpClient, _settingsLoaderMock.Object);

        // Act: We asks the crawler to evaluate 3 different urls on LTU
        await handler.IsAllowedAsync("https://www.ltu.se/forskning");
        await handler.IsAllowedAsync("https://www.ltu.se/om-ltu");
        await handler.IsAllowedAsync("https://www.ltu.se/admin/login");


        // Assert: Controls that the HTTP-client ONLY did one call to LTUs domain
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsoluteUri == "https://www.ltu.se/robots.txt"),
            ItExpr.IsAny<CancellationToken>()
        );
    }


    [Fact]
    public void Constructor_WhenHttpClientIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RobotsHandler(null!, _settingsLoaderMock.Object));
    }


    [Fact]
    public void Constructor_WhenSettingsIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RobotsHandler(_httpClient, null!));
    }


    [Fact]
    public async Task IsAllowed_WhenRobotsTxtDoesNotExist_ReturnsTrue()
    {
        // Arrange: Simulate an 404 Not Found
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var handler = new RobotsHandler(_httpClient, _settingsLoaderMock.Object);

        // Act
        var result = await handler.IsAllowedAsync("https://example.com/some-page");

        // Assert
        Assert.True(result, "Should allow crawling if robots.txt is missing");
    }

    [Fact]
    public async Task IsAllowed_WhenRobotsTxtExceptionRuleIsFound_ReturnsFalse()
    {
        // Arrange

        var sut = new RobotsHandler(_httpClient, _settingsLoaderMock.Object);

        // Act
        var result = await sut.IsAllowedAsync("https://ltu.se/private/");

        // Assert
        Assert.True(result, "Should not be blocked because the domain is in the rule exception configuration");
    }
}

