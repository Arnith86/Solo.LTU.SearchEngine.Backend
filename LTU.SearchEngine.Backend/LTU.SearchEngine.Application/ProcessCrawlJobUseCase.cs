using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Exceptions;
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
	public ICrawler Crawler { get; set; }
	public IDomainValidator DomainValidator { get; set; }
	public IIndexer Indexer { get; set; }

	public ProcessCrawlJobUseCase(
		ICrawler crawler, 
		IDomainValidator domainValidator, 
		IIndexer indexer
		)
	{
		Crawler = crawler ?? throw new ArgumentNullException(nameof(crawler));
		DomainValidator = domainValidator ?? throw new ArgumentNullException(nameof(domainValidator));
		Indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
	}

	/// <inheritdoc/>
	public async Task<CrawlResult> Execute(CrawlJob job)
	{
		ValidateJob(job);

		CrawlResult result = await Crawler.FetchAsync(job.Url);

		if (result is null)
			throw new InvalidOperationException($"Failed to fetch URL: {job.Url}");

		Indexer.Index(result);

		return result;
	}

	private void ValidateJob(CrawlJob job)
	{
		if (job is null)
			throw new ArgumentNullException(nameof(job));

		if (string.IsNullOrWhiteSpace(job.Url))
			throw new ArgumentException("URL must have a value.", nameof(job.Url));
		
		if (!DomainValidator.IsWhitelisted(job.Url))
			throw new DomainNotWhitelistedException(job.Url);
	}
}
