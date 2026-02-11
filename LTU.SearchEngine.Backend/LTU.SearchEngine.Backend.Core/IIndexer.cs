using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public interface IIndexer
{
	public void Index(CrawlResult result);
}
