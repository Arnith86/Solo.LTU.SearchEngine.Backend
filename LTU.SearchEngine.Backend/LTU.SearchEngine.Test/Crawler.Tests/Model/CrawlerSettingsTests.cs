using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.Crawler.Tests.Model;

public class CrawlerSettingsTests
{
	private IReadOnlyList<TimeSpan> retryIntervals;

	public CrawlerSettingsTests()
	{
		retryIntervals = new List<TimeSpan>
		{
			TimeSpan.FromSeconds(3600),    // 01:00:00		// 1 hour
			TimeSpan.FromSeconds(86400),   // 1.00:00:00	// 1 day
			TimeSpan.FromSeconds(604800)   // 7.00:00:00	// 1 week
		};
	}

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
			minDelayMs,
			retryIntervals
		);

		// Assert
		Assert.Equal(userAgent, sut.UserAgent);
		Assert.Equal(maxConcurrencyPerDomain, sut.MaxConcurrencyPerDomain);
		Assert.Equal(minDelayMs, sut.MinDelayMs);
		Assert.Equal(retryIntervals, sut.RetryIntervals);
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
			minDelayMs,
			retryIntervals)
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
			minDelayMs,
			retryIntervals)
		);
	}

	[Fact]
	public void CrawlerSettings_Constructor_RetryIntervals_Null_ThrowsArgumentException()
	{
		// Arrange
		string userAgent = "LTUSearchCrawler/1.0 (Academic project; contact: some.mail@student.ltu.se)";
		int maxConcurrencyPerDomain = 5;
		int minDelayMs = 100;

		// Act & Assert
		Assert.Throws<ArgumentException>(() => new CrawlerSettings(
			userAgent,
			maxConcurrencyPerDomain,
			minDelayMs,
			null!)
		);
	}


	// Tests each index of the retryIntervals list by giving it a zero TimeSpan value.
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void CrawlerSettings_Constructor_RetryIntervals_ZeroInterval_ThrowsArgumentOutOfRangeException(int index)
	{
		// Arrange
		List<TimeSpan> intervals = new List<TimeSpan>
		{
			TimeSpan.FromSeconds(3600),    // 01:00:00		// 1 hour
			TimeSpan.FromSeconds(86400),   // 1.00:00:00	// 1 day
			TimeSpan.FromSeconds(604800)   // 7.00:00:00	// 1 week
		};

		intervals[index] = TimeSpan.Zero;
		
		string userAgent = "LTUSearchCrawler/1.0 (Academic project; contact: some.mail@student.ltu.se)";
		int maxConcurrencyPerDomain = 5;
		int minDelayMs = 100;

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => new CrawlerSettings(
			userAgent,
			maxConcurrencyPerDomain,
			minDelayMs,
			intervals)
		);
	}

	// Tests each index of the retryIntervals list by giving it a negative TimeSpan value.
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void CrawlerSettings_Constructor_RetryIntervals_NegativeTime_ThrowsArgumentOutOfRangeException(int index)
	{
		// Arrange
		List<TimeSpan> intervals = new List<TimeSpan>
		{
			TimeSpan.FromSeconds(3600),    // 01:00:00		// 1 hour
			TimeSpan.FromSeconds(86400),   // 1.00:00:00	// 1 day
			TimeSpan.FromSeconds(604800)   // 7.00:00:00	// 1 week
		};

		intervals[index] = TimeSpan.FromSeconds(-1);

		string userAgent = "LTUSearchCrawler/1.0 (Academic project; contact: some.mail@student.ltu.se)";
		int maxConcurrencyPerDomain = 5;
		int minDelayMs = 100;

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => new CrawlerSettings(
			userAgent,
			maxConcurrencyPerDomain,
			minDelayMs,
			intervals)
		);
	}
}
