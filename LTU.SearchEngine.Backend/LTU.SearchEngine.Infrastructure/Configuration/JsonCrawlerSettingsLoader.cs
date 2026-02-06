using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace LTU.SearchEngine.Infrastructure.Configuration;

public class JsonCrawlerSettingsLoader : ICrawlerSettingsLoader
{
	private IConfiguration _configuration;

	public JsonCrawlerSettingsLoader(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public CrawlerSettings Load() 
	{
		var dto = _configuration
			.GetSection("CrawlerSettings")
			.Get<CrawlerSettingsDTO>() 
			?? throw new InvalidOperationException("CrawlerSettings section is missing from the configuration.");

		return new CrawlerSettings(
			userAgent: dto.UserAgent,
			maxConcurrencyPerDomain: dto.MaxConcurrencyPerDomain,
			minDelayMs: dto.MinDelayMs,
			retryIntervals: dto.RetryIntervals
		);
	}
}
