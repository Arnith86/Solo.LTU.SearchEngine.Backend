using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.HelperClasses;

public class CrawlerSettingsBuilder
{
    public static CrawlerSettings BuildCrawlerSettings(
        string userAgent = "test-agent",
        int maxConcurrencyPerDomain = 2,
        int minDelayMs = 100
        )
	{
		return new CrawlerSettings(
			userAgent: userAgent,
			maxConcurrencyPerDomain: maxConcurrencyPerDomain,
			minDelayMs: minDelayMs,
			retryIntervals: new List<TimeSpan>
			{
				TimeSpan.FromMilliseconds(50),
				TimeSpan.FromMilliseconds(150),
				TimeSpan.FromMilliseconds(200)
			},
			crawlUpdateInterval: TimeSpan.FromMilliseconds(200),
            seedUrls: new List<string> { "ltu.se" },
			whiteList: new List<string> { "ltu.se" },
            robotsExceptionRules: new Dictionary<string, List<string>>{
                { "ltu.se", new List<string> { "/private/" } }
            }
        );
	}

    public static CrawlerSettings BuildCrawlerSettings(
        List<TimeSpan> retryIntervals,
        TimeSpan crawlUpdateInterval,
        List<string> seedUrls,
		List<string> whiteList,
        Dictionary<string, List<string>> robotsExceptionRules,
        string userAgent = "test-agent",
        int maxConcurrencyPerDomain = 2,
        int minDelayMs = 100
        )
	{
		return new CrawlerSettings(
			userAgent: userAgent,
			maxConcurrencyPerDomain: maxConcurrencyPerDomain,
			minDelayMs: minDelayMs,
			retryIntervals: retryIntervals,
			crawlUpdateInterval: crawlUpdateInterval,
            seedUrls: seedUrls,
			whiteList: whiteList,
            robotsExceptionRules: robotsExceptionRules
        );
	}
}