using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Repositories;

namespace LTU.SearchEngine.Infrastructure.Indexing;

/// <summary>
/// Orchestrates the indexing flow for crawl results and manages document metadata.
/// </summary>
/// <remarks>
///     <para>
///         The indexer acts as a coordinator between the transformation logic (<see cref="IIndexingPipeline"/>) 
///         and the persistence layer (<see cref="IIndexRepository"/>).
///     </para>
///     <para>
///         Beyond initial indexing, it provides functionality to support incremental crawling by 
///         identifying existing documents through content hashes and updating crawl timestamps.
/// <   /para>
/// </remarks>
public class Indexer : IIndexer
{
    private readonly IIndexRepository _repository;
    private readonly IIndexingPipeline _pipeline;
    public Indexer(IIndexRepository repository, IIndexingPipeline pipeline)
    {
        _repository = repository;
        _pipeline = pipeline;
    }

    /// <inheritdoc/>
    public async Task IndexAsync(CrawlResult crawlResult)
    {
        if (crawlResult is null)
            throw new ArgumentNullException(nameof(crawlResult));
        
        var document = _pipeline.Transform(crawlResult);

        await _repository.AddDocumentAsync(document);
    }

    /// <inheritdoc/>
    public async Task<int?> GetExistingDocumentIdAsync(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) 
            throw new ArgumentException(nameof(hash), " must have a value.");

        return await _repository.GetExistingDocumentByHashAsync(hash);
    }

    /// <inheritdoc/>
    public async Task UpdateIndexCrawlTimeAsync(int documentId, DateTime newCrawl) =>
        await _repository.UpdateLastCrawledAsync(documentId, newCrawl);
}
