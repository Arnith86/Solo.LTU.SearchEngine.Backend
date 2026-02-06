namespace LTU.SearchEngine.Backend.Core.Model;

public class CrawlerSettingsDTO
{
	public string UserAgent { get; }
	public int MaxConcurrencyPerDomain { get; }
	public int MinDelayMs { get; }
	public IReadOnlyList<TimeSpan> RetryIntervals { get; }

	public CrawlerSettingsDTO(
		string userAgent,
		int maxConcurrencyPerDomain,
		int minDelayMs,
		IReadOnlyList<TimeSpan> retryIntervals
		)
	{
		UserAgent = userAgent;
		MaxConcurrencyPerDomain = maxConcurrencyPerDomain;
		MinDelayMs = minDelayMs;
		RetryIntervals = retryIntervals;
	}
}
