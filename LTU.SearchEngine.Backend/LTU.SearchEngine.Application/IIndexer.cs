using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application;

public interface IIndexer
{
	public void Index(CrawlResult result);
}
