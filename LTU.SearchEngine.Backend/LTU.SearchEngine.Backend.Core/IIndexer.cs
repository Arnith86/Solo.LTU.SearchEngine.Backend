using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public interface IIndexer
{
    Task IndexAsync(CrawlResult crawlResult);
    Task<bool> IsAlreadyIndexedAsync(string hash);
}
