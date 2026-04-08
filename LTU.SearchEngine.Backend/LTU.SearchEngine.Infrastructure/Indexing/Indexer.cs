using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Repositories;

namespace LTU.SearchEngine.Infrastructure.Indexing;

/// <summary>
/// Orchestrates the indexing flow for crawl results.
/// </summary>
/// <remarks>
/// <para>
/// The indexer is responsible for receiving <see cref="CrawlResult"/> instances,
/// validating whether indexing should occur, invoking the indexing pipeline,
/// and persisting the resulting index documents via a repository.
/// </para>
/// <para>
/// This class coordinates the indexing process but does not perform text normalization,
/// term frequency calculation, or direct storage operations.
/// </para>
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

    public async Task IndexAsync(CrawlResult crawlResult)
    {
        if (crawlResult is null)
            throw new ArgumentNullException(nameof(crawlResult));
        
        var document = _pipeline.Transform(crawlResult);

        await _repository.AddDocumentAsync(document);
    }

    public async Task<bool> IsAlreadyIndexedAsync(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) 
            throw new ArgumentException(nameof(hash), " must have a value.");

        return await _repository.IsDocumentDuplicateAsync(hash);
    }
}
