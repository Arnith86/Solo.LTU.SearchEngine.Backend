using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Indexing.Repositories;

namespace LTU.SearchEngine.Infrastructure.Indexing
{
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
        private readonly IndexingPipeline _pipeline;
        public Indexer(IIndexRepository repository, IndexingPipeline pipeline) 
        {
            _repository = repository;
            _pipeline = pipeline;
        }

        public void Index(CrawlResult crawlResult)
        {
            if (crawlResult is null)
                throw new ArgumentNullException(nameof(crawlResult));

            var document = _pipeline.TransForm(crawlResult);
            _repository.Save(document);
        }
    }
}
