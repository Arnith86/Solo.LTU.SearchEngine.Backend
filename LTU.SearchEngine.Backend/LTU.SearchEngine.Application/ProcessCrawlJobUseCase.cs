using System.Diagnostics;
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
	public async Task<ProcessJobResponse> Execute(CrawlJob job)
	{
		await ValidateJob(job);
		
		DateTime fetchStartTime = DateTime.UtcNow;
		var stopwatch = Stopwatch.StartNew();

		try
        {
            var rawData = await _crawler.FetchRawAsync(job.Url);

            var hash = await _crawler.GetContentHash(rawData);
            int? documentId = await _indexer.GetExistingDocumentIdAsync(hash);


            if (documentId is not null)
            {
                await _indexer.UpdateIndexCrawlTimeAsync((int)documentId, fetchStartTime);
                return new ProcessJobResponse(ChangedContent: false, ProcessedAt: fetchStartTime, CrawlResult: null);
            }

            ProcessJobResponse response = await CreateProcessJobResponse(
				changedContent: true,
				processedAt: fetchStartTime, 
				crawlResult: await _crawler.FetchAsync(rawData, hash)
			);

            if (response.CrawlResult is null)
                throw new InvalidOperationException($"Failed to fetch URL: {job.Url}");

            await _indexer.IndexAsync(response.CrawlResult);

            return response;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
			/// ToDo: LOGG THIS $"HTTP request failed: {ex.Message}"
        
            return await CreateFailedRequestResponse(job, fetchStartTime, stopwatch, ex);
        }
        finally {stopwatch.Stop();}
	}


    private async Task<ProcessJobResponse> CreateFailedRequestResponse(
		CrawlJob job, 
		DateTime fetchStartTime,
		Stopwatch stopwatch,
		HttpRequestException ex
	)
    {
        var statusCode = ex.StatusCode ?? System.Net.HttpStatusCode.ServiceUnavailable;

        CrawlResult crawlResult = _crawler.CreateErrorResult(
            job.Url,
            statusCode,
            stopwatch.ElapsedMilliseconds,
            fetchStartTime
        );

        return await CreateProcessJobResponse(changedContent: false, fetchStartTime, crawlResult);
    }


    private async Task<ProcessJobResponse> CreateProcessJobResponse(
		bool changedContent,
		DateTime processedAt,
		CrawlResult crawlResult
	)
    {
        return new ProcessJobResponse(
            ChangedContent: changedContent,
            ProcessedAt: processedAt,
            CrawlResult: crawlResult
        );
    }


    private async Task ValidateJob(CrawlJob job)
    {
        if (job is null)
            throw new ArgumentNullException(nameof(job));

        if (string.IsNullOrWhiteSpace(job.Url))
            throw new ArgumentException("URL must have a value.", nameof(job.Url));
    }
}
