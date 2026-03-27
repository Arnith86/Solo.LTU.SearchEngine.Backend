using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Crawling;

namespace LTU.SearchEngine.Application;

/// <summary>
/// Concrete implementation of <see cref="IProcessCrawlJobUseCase"/>. <br />
/// Processes a crawl job by fetching content, validating domains.
/// </summary>
/// <remarks>
/// <para>
/// This use case is responsible for executing the full lifecycle of a crawl job:
/// it validates the <see cref="CrawlJob"/>, fetches the content using an <see cref="ICrawler"/>,
/// and passes the fetched content to an <see cref="IIndexer"/> for indexing.
/// </para>
/// <para>
/// Exceptions are thrown for invalid jobs, non-whitelisted domains, or failed fetches.
/// </para>
/// </remarks>
public class ProcessCrawlJobUseCase : IProcessCrawlJobUseCase
{
	private ICrawler _crawler;
	private IIndexer _indexer;

	public ProcessCrawlJobUseCase(ICrawler crawler, IIndexer indexer)
	{
		_crawler = crawler ?? throw new ArgumentNullException(nameof(crawler));
		_indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
	}

	/// <inheritdoc/>
	public async Task<CrawlResult> Execute(CrawlJob job)
	{
		await ValidateJob(job);

		CrawlResult result = await _crawler.FetchAsync(job.Url);

		if (result is null)
			throw new InvalidOperationException($"Failed to fetch URL: {job.Url}");

		await _indexer.IndexAsync(result);

		return result;
	}

	private async Task ValidateJob(CrawlJob job)
	{
		if (job is null)
			throw new ArgumentNullException(nameof(job));

		if (string.IsNullOrWhiteSpace(job.Url))
			throw new ArgumentException("URL must have a value.", nameof(job.Url));		
	}
}
