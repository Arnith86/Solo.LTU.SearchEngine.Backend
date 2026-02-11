using LTU.SearchEngine.Backend.Core.Model.Entities;

namespace LTU.SearchEngine.BackgroundServices;

public class TplCrawlJobDispatcher : ICrawlJobDispatcher
{
	public Task Enqueue(CrawlJob job)
	{
		throw new NotImplementedException();
	}

	public Task Start(CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}
