using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application;

public interface IProcessCrawlJobUseCase
{
	public Task<CrawlResult> Execute(CrawlJob job);  
}
