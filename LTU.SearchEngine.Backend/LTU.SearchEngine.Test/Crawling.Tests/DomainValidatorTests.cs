using J2N;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Configuration;
using LTU.SearchEngine.Test.HelperClasses;
using Moq;

namespace LTU.SearchEngine.Test.Crawling;

public class DomainValidatorTests
{
    private readonly DomainValidator _sut;

    public DomainValidatorTests()
    {
        var mockCrawlerSettingsLoader = new Mock<ICrawlerSettingsLoader>();

        CrawlerSettings crawlerSettings = CrawlerSettingsBuilder.BuildCrawlerSettings
        (
            userAgent: "TestAgent",
            seedUrls: new List<string> { "ltu.se" },
            whiteList: new List<string> { "ltu.se", "umu.se" },
            maxConcurrencyPerDomain: 1, 
            minDelayMs: 0,
            retryIntervals: new List<TimeSpan> { TimeSpan.FromSeconds(3600)}, 
            crawlUpdateInterval: TimeSpan.FromMilliseconds(200),
            robotsExceptionRules: new Dictionary<string, List<string>>{
                { "ltu.se", new List<string> { "/private/" } }
            }
        );

        mockCrawlerSettingsLoader.Setup(csl => csl.Load()).Returns(crawlerSettings);

        _sut = new DomainValidator(mockCrawlerSettingsLoader.Object);
    }

    [Theory]
    // Allowed
    [InlineData("https://ltu.se", true)]
    [InlineData("http://ltu.se", true)]
    [InlineData("http://www.ltu.se", true)]
    [InlineData("http://student.ltu.se", true)]
    [InlineData("http://sub.department.ltu.se", true)]
    [InlineData("http://LTU.SE", true)]
    [InlineData("http://umu.SE", true)]
    [InlineData("http://www.umu.se", true)]
    // Not allowed
    [InlineData("http://www.google.com", false)]
    [InlineData("http://evil-ltu.com", false)]
    [InlineData("not-an-url", false)]
    [InlineData("/relative/path", false)]
    public void IsWhitelisted_ShouldValidateCorrecty(string url, bool expectedResult)
    {
        // Act 
        bool result = _sut.IsWhitelisted(url);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void IsWhitelisted_ShouldReturnFalse_WhenUrlIsMalformed()
    {
        // Arrange 
        string url = "http://";

        // Act 
        bool result = _sut.IsWhitelisted(url);

        // Assert
        Assert.False(result);
    }
}