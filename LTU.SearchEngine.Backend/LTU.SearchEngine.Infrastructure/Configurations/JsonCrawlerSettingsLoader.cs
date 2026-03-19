using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LTU.SearchEngine.Infrastructure.Configuration;

/// <summary>
/// Loads crawler settings from a JSON configuration source via <see cref="IConfiguration"/>. <br />
/// Implements the <see cref="ICrawlerSettingsLoader"/> interface.
/// </summary>
/// <remarks>
/// This implementation utilizes <see cref="IOptionsMonitor{TOptions}"/> to support  <br />
/// **hot-swapping**, allowing configuration changes in the underlying JSON file <br />
/// to be reflected in real-time without restarting the application.
/// </remarks>

public class JsonCrawlerSettingsLoader : ICrawlerSettingsLoader
{
	private readonly IOptionsMonitor<CrawlerSettingsDTO> _monitor;

	public JsonCrawlerSettingsLoader(IOptionsMonitor<CrawlerSettingsDTO> monitor)
	{
		_monitor = monitor;
	}

	/// <inheritdoc/>
	public CrawlerSettings Load() 
	{
		var dto = _monitor.CurrentValue;

		if (dto is null) 
			throw new InvalidOperationException("CrawlerSettings section is missing from the configuration.");

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
