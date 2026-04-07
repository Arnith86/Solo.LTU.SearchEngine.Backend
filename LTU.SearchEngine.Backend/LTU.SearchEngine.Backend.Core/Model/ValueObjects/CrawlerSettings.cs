namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>
/// Represents immutable configuration settings that control how the crawler behaves,
/// such as HTTP identification, concurrency limits, request delays, and retry strategy.
/// </summary>
/// <remarks>This type is a value object and should be treated as immutable.</remarks>
public class CrawlerSettings
{
	public string UserAgent { get; }
	public int MaxConcurrencyPerDomain { get; } 
	public int MinDelayMs { get; }
	public IReadOnlyList<TimeSpan> RetryIntervals { get; }
	public TimeSpan CrawlUpdateInterval { get; }
    public IReadOnlyList<string> SeedUrls { get; }
    public IReadOnlyList<string> WhiteList { get; } 
    public Dictionary<string, List<string>>? RobotsExceptionRules { get; set; } = new();


    /// <summary>Initializes a new instance of the <see cref="CrawlerSettings"/> class.</summary>
    /// <param name="userAgent">The User-Agent string used for outgoing HTTP requests.</param>
    /// <param name="maxConcurrencyPerDomain">Maximum number of concurrent crawl requests allowed per domain. Must be greater than 0.</param>
    /// <param name="minDelayMs">Minimum delay (in milliseconds) between requests to the same domain. Must be 0 or greater.</param>
    /// <param name="retryIntervals">Retry delays used for transient failures. Must contain at least one positive <see cref="TimeSpan"/>.</param>
	/// <param name="seedUrls">The base urls used to start the crawling process. </param>
	/// <param name="whiteList">Contains the domains that the crawler is allowed to visit.</param>
	/// <param name="CrawlUpdateInterval">Specify the time between new crawls of already crawled pages.</param>
    /// <param name="robotsExceptionRules">Optional. A dictionary of domains and their corresponding exception rules for robots.txt. Defaults to null.</param>
	/// <exception cref="ArgumentException">
    /// Thrown when <paramref name="userAgent"/> is null/empty/whitespace, <br />
    /// or when <paramref name="retryIntervals"/> is null/empty,<br />
    /// or when <paramref name="retryIntervals"/> contains non-positive values.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxConcurrencyPerDomain"/> is less than or equal to 0, <br />
    /// or when <paramref name="minDelayMs"/> is negative.
    /// </exception>
    public CrawlerSettings(
		string userAgent, 
		int maxConcurrencyPerDomain, 
		int minDelayMs,
		IReadOnlyList<TimeSpan> retryIntervals,
        IReadOnlyList<string> seedUrls,
		IReadOnlyList<string> whiteList,
		TimeSpan crawlUpdateInterval,
		Dictionary<string, List<string>>? robotsExceptionRules = null
       )
	{
		if (string.IsNullOrWhiteSpace(userAgent))
			throw new ArgumentException("UserAgent must be provided.", nameof(userAgent));

		if (maxConcurrencyPerDomain <= 0)
			throw new ArgumentOutOfRangeException(
				nameof(maxConcurrencyPerDomain) + " must be greater than zero."
			);

		if (minDelayMs < 0)
			throw new ArgumentOutOfRangeException(nameof(minDelayMs) + " cannot have a negative value.");

		if (retryIntervals is null || retryIntervals.Count == 0)
			throw new ArgumentException(nameof(retryIntervals), "must contain at least one time interval!");

		if (retryIntervals.Any(x => x <= TimeSpan.Zero))
			throw new ArgumentOutOfRangeException(nameof(retryIntervals), " cannot not contain negative values!");

		if (crawlUpdateInterval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(crawlUpdateInterval), " cannot not be zero or negative values!");

        // Validate that the list exists (FRQ-1003 whitelist)
        if (seedUrls is null || seedUrls.Count == 0)
            throw new ArgumentException("Must provide at least one seed URL/Domain.", nameof(seedUrls));

		if (whiteList is null || whiteList.Count == 0)
			throw new ArgumentException("Must provide at least one URL/Domain.", nameof(whiteList));


        UserAgent = userAgent; 
		MaxConcurrencyPerDomain = maxConcurrencyPerDomain;
		MinDelayMs = minDelayMs;
		RetryIntervals = retryIntervals;
        SeedUrls = seedUrls;
		WhiteList = whiteList;
		CrawlUpdateInterval = crawlUpdateInterval;
		RobotsExceptionRules = robotsExceptionRules ?? new Dictionary<string, List<string>>();
    }

	/// <summary>Returns the retry delay to use for a given attempt number.</summary>
	/// <param name="attemptNumber">The retry attempt number. Must be greater than 0.</param>
	/// <returns>The delay interval to wait before retrying.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="attemptNumber"/> is less than or equal to 0.
	/// </exception>
	/// <remarks>
	/// If <paramref name="attemptNumber"/> exceeds the number of configured intervals,
	/// the last configured interval is returned.
	/// </remarks>
	public TimeSpan GetRetryDelayInterval(int attemptNumber)
	{
		if (attemptNumber <= 0)
			throw new ArgumentOutOfRangeException(nameof(attemptNumber), " cannot be a value less then 1!");

		// If attempts exceed configured allowed interval, chose the longest allowed retry interval.
		int index = int.Min(attemptNumber - 1, RetryIntervals.Count - 1);
		
		return RetryIntervals[index];
	}
}



