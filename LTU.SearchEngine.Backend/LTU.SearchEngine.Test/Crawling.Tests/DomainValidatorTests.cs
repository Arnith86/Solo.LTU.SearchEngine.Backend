using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure;

namespace LTU.SearchEngine.Test.Crawling;

public class DomainValidatorTests
{
    private readonly DomainValidator _sut;

    public DomainValidatorTests()
    {
        CrawlerSettings crawlerSettings = new CrawlerSettings
        (
            userAgent: "TestAgent",
            seedUrls: new List<string> { "ltu.se" },
            whiteList: new List<string> { "ltu.se", "umu.se" },
            maxConcurrencyPerDomain: 1, 
            minDelayMs: 0,
            retryIntervals: new List<TimeSpan> { TimeSpan.FromSeconds(3600)} 
        );

        _sut = new DomainValidator(crawlerSettings);
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