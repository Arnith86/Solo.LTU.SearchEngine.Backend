using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.Crawler.Tests.Model;

public class CrawlerSettingsTests
{
	[Theory]
	[InlineData(1, 0)]
	[InlineData(5, 100)]
	public void CrawlerSettings_ValidParameters_ShouldCreateInstance(
		int maxConcurrencyPerDomain, int minDelayMs)
	{
		// Arrange
		string userAgent = "LTUSearchCrawler/1.0 (Academic project; contact: some.mail@student.ltu.se)";

		// Act
		CrawlerSettings sut = new CrawlerSettings(
			userAgent,
			maxConcurrencyPerDomain,
			minDelayMs
		);

		// Assert
		Assert.Equal(userAgent, sut.UserAgent);
		Assert.Equal(maxConcurrencyPerDomain, sut.MaxConcurrencyPerDomain);
		Assert.Equal(minDelayMs, sut.MinDelayMs);
	}

	[Fact]
	public void CrawlerSettings_Constructor_WithNullUserAgent_ThrowsArgumentException()
	{
		// Arrange
		string userAgent = null!;
		int maxConcurrencyPerDomain = 5;
		int minDelayMs = 100;

		// Act & Assert
		Assert.Throws<ArgumentException>(() => new CrawlerSettings(
			userAgent!,
			maxConcurrencyPerDomain,
			minDelayMs)
		);
	}

	[Theory]
	[InlineData(-1, 100)]   // maxConcurrencyPerDomain <= 0
	[InlineData(0, 100)]    // maxConcurrencyPerDomain <= 0	
	[InlineData(5, -1)]     // minDelayMs < 0
	public void CrawlerSettings_Constructor_OutOfRangeArguments_ThrowsArgumentOutOfRangeException(
		int maxConcurrencyPerDomain, int minDelayMs)
	{
		// Arrange
		string userAgent = "LTUSearchCrawler/1.0 (Academic project; contact: some.mail@student.ltu.se)";

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => new CrawlerSettings(
			userAgent,
			maxConcurrencyPerDomain,
			minDelayMs)
		);
	}
}
