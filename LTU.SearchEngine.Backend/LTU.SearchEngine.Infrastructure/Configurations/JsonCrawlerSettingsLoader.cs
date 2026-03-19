using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace LTU.SearchEngine.Infrastructure.Configuration;

/// <summary>
/// Loads crawler settings from a JSON configuration source via <see cref="IConfiguration"/>. <br />
/// Implements the <see cref="ICrawlerSettingsLoader"/> interface.
/// </summary>
/// <remarks>
/// Expects a configuration section named "CrawlerSettings" that can be mapped <br />
/// to a <see cref="CrawlerSettingsDTO"/>. Throws an <see cref="InvalidOperationException"/> <br />
/// if the section is missing.
/// </remarks>
public class JsonCrawlerSettingsLoader : ICrawlerSettingsLoader
{
	private IConfiguration _configuration;

	public JsonCrawlerSettingsLoader(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	/// <inheritdoc/>
	public CrawlerSettings Load() 
	{
		var dto = _configuration
			.GetSection("CrawlerSettings")
			.Get<CrawlerSettingsDTO>() 
			?? throw new InvalidOperationException("CrawlerSettings section is missing from the configuration.");

		return new CrawlerSettings(
			userAgent: dto.UserAgent!,
			maxConcurrencyPerDomain: dto.MaxConcurrencyPerDomain,
			minDelayMs: dto.MinDelayMs,
			retryIntervals: dto.RetryIntervals!,
            seedUrls: dto.SeedUrls ?? new List<string>(),
			whiteList: dto.WhiteList ?? new List<string>() 
        );
	}
}
