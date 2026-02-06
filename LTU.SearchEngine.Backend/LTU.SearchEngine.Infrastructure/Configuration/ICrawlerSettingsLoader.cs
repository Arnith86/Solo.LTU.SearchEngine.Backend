using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace LTU.SearchEngine.Infrastructure.Configuration;

public interface ICrawlerSettingsLoader
{
	CrawlerSettings Load();
}
