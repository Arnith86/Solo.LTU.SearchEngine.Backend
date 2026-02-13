using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.Crawling.Tests.Model;

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

	public CrawlerSettings CreateSut(
		string userAgent = "LTUSearchCrawler/1.0 (Academic project; contact: some.mail@student.ltu.se)",
		int maxConcurrencyPerDomain = 5,
		int minDelayMs = 100
		)
	{
		
		return new CrawlerSettings(
			userAgent,
			maxConcurrencyPerDomain,
			minDelayMs,
			retryIntervals
		);
	}

	[Theory]
	[InlineData("TestAgent", 1, 0)]
	[InlineData("Test Agent", 5, 100)]
	public void CrawlerSettings_ValidParameters_ShouldCreateInstance(
		string userAgent, 
		int maxConcurrencyPerDomain, 
		int minDelayMs)
	{
		// Arrange & Act
		CrawlerSettings sut = CreateSut(userAgent, maxConcurrencyPerDomain, minDelayMs);

		// Assert
		Assert.Equal(userAgent, sut.UserAgent);
		Assert.Equal(maxConcurrencyPerDomain, sut.MaxConcurrencyPerDomain);
		Assert.Equal(minDelayMs, sut.MinDelayMs);
		Assert.Equal(retryIntervals, sut.RetryIntervals);
	}

	[Fact]
	public void CrawlerSettings_Constructor_WithNullUserAgent_ThrowsArgumentException()
	{
		// Act & Assert
		Assert.Throws<ArgumentException>(() => CreateSut(userAgent: null!));
	}

	[Theory]
	[InlineData(-1, 100)]   // maxConcurrencyPerDomain <= 0
	[InlineData(0, 100)]    // maxConcurrencyPerDomain <= 0	
	[InlineData(5, -1)]     // minDelayMs < 0
	public void CrawlerSettings_Constructor_OutOfRangeArguments_ThrowsArgumentOutOfRangeException(
		int maxConcurrencyPerDomain, int minDelayMs)
	{
		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => CreateSut(
			maxConcurrencyPerDomain: maxConcurrencyPerDomain,
			minDelayMs: minDelayMs
			) 
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

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void GetRetryDelayInterval_ValidAttempt_ShouldPass(int attemptNr)
	{
		// Arrange
		CrawlerSettings sut = CreateSut();

		// Act
		TimeSpan delay = sut.GetRetryDelayInterval(attemptNr);

		// Assert
		Assert.Equal(retryIntervals[attemptNr - 1], delay);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-100)]
	public void GetRetryDelayInterval_LessThenAllowed_ShouldThrow_ArgumentOutOfRangeException(int attemptNr)
	{
		// Arrange
		CrawlerSettings sut = CreateSut();

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => sut.GetRetryDelayInterval(attemptNr)); 
	}

	[Theory]
	[InlineData(4)]
	[InlineData(100)]
	[InlineData(100000)]
	public void GetRetryDelayInterval_MoreThenAllowed_ShouldThrow_ArgumentOutOfRangeException(int attemptNr)
	{
		// Arrange
		CrawlerSettings sut = CreateSut();

		// Act
		TimeSpan delay = sut.GetRetryDelayInterval(attemptNr);

		// Assert
		Assert.Equal(retryIntervals[2], delay);

	}
}
