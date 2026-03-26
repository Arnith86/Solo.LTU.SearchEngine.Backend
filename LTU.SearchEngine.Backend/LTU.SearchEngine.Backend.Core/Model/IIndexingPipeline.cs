using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Backend.Core.Model;

public interface IIndexingPipeline
{
    IndexDocument Transform(CrawlResult crawlResult);
}
