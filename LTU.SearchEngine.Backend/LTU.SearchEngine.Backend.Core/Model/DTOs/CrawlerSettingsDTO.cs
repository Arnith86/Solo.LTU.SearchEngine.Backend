namespace LTU.SearchEngine.Backend.Core.Model.DTOs;

public class CrawlerSettingsDTO
{
	public string? UserAgent { get; set; }
	public int MaxConcurrencyPerDomain { get; set; }
	public int MinDelayMs { get; set; }
	public List<TimeSpan>? RetryIntervals { get; set; }
	public TimeSpan CrawlUpdateInterval { get; set; }
    public List<string>? SeedUrls { get; set; }
	public List<string>? WhiteList { get; set; }
	public Dictionary<string, List<string>>? RobotsExceptionRules { get; set; }
}
