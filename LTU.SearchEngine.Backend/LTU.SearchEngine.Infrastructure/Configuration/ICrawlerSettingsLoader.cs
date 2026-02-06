using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Infrastructure.Configuration;

public interface ICrawlerSettingsLoader
{
	CrawlerSettings Load();
}
