using LTU.SearchEngine.Backend.Core.Model.Entities;

namespace LTU.SearchEngine.BackgroundServices;

public interface ICrawlJobDispatcher
{
	public Task Enqueue(CrawlJob job);
	public Task Start(CancellationToken ct);
}
