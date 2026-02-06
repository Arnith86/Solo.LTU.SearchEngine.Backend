namespace LTU.SearchEngine.Backend.Core.Model;

public class CrawlerSettingsDTO
{
	public string? UserAgent { get; set; }
	public int MaxConcurrencyPerDomain { get; set; }
	public int MinDelayMs { get; set; }
	public List<TimeSpan>? RetryIntervals { get; set; }
	
}
