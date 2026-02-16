using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Infrastructure.Configuration;

/// <summary>
/// Defines a contract for classes responsible for loading <see cref="CrawlerSettings"/>.
/// Implementations may load settings from configuration files, databases, or other sources.
/// </summary>
public interface ICrawlerSettingsLoader
{
	/// <summary>Loads the crawler settings.</summary>
	/// <returns>
	/// A <see cref="CrawlerSettings"/> object containing configuration values <br />
	/// for the crawler, such as user agent, concurrency limits, and crawl delays.
	/// </returns>
	CrawlerSettings Load();
}
