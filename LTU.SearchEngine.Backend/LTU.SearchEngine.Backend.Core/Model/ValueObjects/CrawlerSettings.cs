namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public class CrawlerSettings
{
	public string UserAgent { get; }
	public int MaxConcurrencyPerDomain { get; } 
	public int MinDelayMs { get; }

	public CrawlerSettings(
		string userAgent, 
		int maxConcurrencyPerDomain, 
		int minDelayMs
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
		UserAgent = userAgent; 
		MaxConcurrencyPerDomain = maxConcurrencyPerDomain;
		MinDelayMs = minDelayMs;
	}
}
