using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.HelperClasses;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Infrastructure.Indexing;

/// <summary>Performs the transformation of crawl results into index-ready documents.</summary>
/// <remarks>
/// <para>
/// The indexing pipeline is a pure transformation component that converts a <br />
/// <see cref="CrawlResult"/> into an <see cref="IndexDocument"/> by normalizing terms <br />
/// and calculating term frequency per document field.
/// </para>
/// </remarks>
public class IndexingPipeline : IIndexingPipeline
{
    private readonly ITextNormalizer<string> _textNormalizer;

    /// <summary>Initializes a new instance of the <see cref="IndexingPipeline"/> class.</summary>
    /// <param name="normalizer">The strategy used to normalize terms (e.g., lowercasing, stemming, or removing punctuation).</param>
    public IndexingPipeline(ITextNormalizer<string> normalizer)
    {
        _textNormalizer = normalizer;
    }

    /// <inheritdoc/>
    /// <param name="crawlResult">The result containing the URL, raw terms, and content hash.</param>
    /// <returns>An <see cref="IndexDocument"/> containing read-only maps of normalized terms and their frequencies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="crawlResult"/> is null.</exception>
    public virtual IndexDocument Transform(CrawlResult crawlResult)
    {
        if (crawlResult == null) throw new ArgumentNullException(nameof(crawlResult));

        Dictionary<TermSource, TermFrequencyMap> sourceFrequencyMap = new()
        {
            { TermSource.Title, new TermFrequencyMap() },
            { TermSource.Header, new TermFrequencyMap() },
            { TermSource.Body, new TermFrequencyMap() }  
        };

        Dictionary<TermSource, TermPositionMap> sourcePositionMap = new()
        {
            { TermSource.Title, new TermPositionMap() },
            { TermSource.Header, new TermPositionMap() },
            { TermSource.Body, new TermPositionMap() }  
        };

        foreach (var indexedTerm in crawlResult.IndexedTerms)
        {
            var normalized = _textNormalizer.Normalize(indexedTerm.Term, crawlResult.Language);

            if (string.IsNullOrWhiteSpace(normalized)) continue;
            
            sourceFrequencyMap[indexedTerm.Source].AddTerm(normalized);
            sourcePositionMap[indexedTerm.Source].AddTerm(normalized);
        }

        return new IndexDocument(
            url: crawlResult.Url,
            title: crawlResult.Title!,
            language: crawlResult.Language,
            outgoingLinks: crawlResult.ExtractedLinks,
            titleTerms: sourceFrequencyMap[TermSource.Title].ToReadOnly(), 
            headerTerms: sourceFrequencyMap[TermSource.Header].ToReadOnly(),
            contentTerms: sourceFrequencyMap[TermSource.Body].ToReadOnly(),
            titleTermPositions: sourcePositionMap[TermSource.Title].ToReadOnly(),
            headerTermPositions: sourcePositionMap[TermSource.Header].ToReadOnly(),
            contentTermPositions: sourcePositionMap[TermSource.Body].ToReadOnly(),
            contentHash: crawlResult.ContentHash,
            lastCrawl: crawlResult.CrawledAt
        ); 
    }
}