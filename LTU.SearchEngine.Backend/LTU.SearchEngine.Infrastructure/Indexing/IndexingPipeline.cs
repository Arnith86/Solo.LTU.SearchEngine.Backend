using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.HelperClasses;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Infrastructure.Indexing;

/// <summary>
/// Performs the transformation of raw crawl results into structured, index-ready documents.
/// </summary>
/// <remarks>
/// The indexing pipeline acts as a data transformer that converts a <see cref="CrawlResult"/> 
/// into an <see cref="IndexDocument"/>. It orchestrates the normalization process, which may 
/// expand a single raw term into multiple searchable tokens, and calculates frequency and 
/// positional data for each field (Title, Header, Body).
/// </remarks>
public class IndexingPipeline : IIndexingPipeline
{
    private readonly ITextNormalizer<string, IEnumerable<string>> _textNormalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexingPipeline"/> class.
    /// </summary>
    /// <param name="normalizer">
    /// The normalization strategy used to process terms (e.g., handling linguistic splits, 
    /// technical symbols, and lowercasing).
    /// </param>
    public IndexingPipeline(ITextNormalizer<string, IEnumerable<string>> normalizer)
    {
        _textNormalizer = normalizer;
    }

    
    /// <inheritdoc/>
    /// <param name="crawlResult">The result containing the URL, raw extracted terms, and content metadata.</param>
    /// <returns>
    /// An <see cref="IndexDocument"/> containing read-only maps of normalized tokens, 
    /// their frequencies, and their positions within the source document.
    /// </returns>
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
            var normalizedWords = _textNormalizer.Normalize(indexedTerm.Term, crawlResult.Language);

            foreach (var token in normalizedWords)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;
            
                sourceFrequencyMap[indexedTerm.Source].AddTerm(token);
                sourcePositionMap[indexedTerm.Source].AddTerm(token);    
            }
        }

        return new IndexDocument(
            url: crawlResult.Url,
            title: crawlResult.Title!,
            language: crawlResult.Language,
            documentMetaData: crawlResult.MetaData, 
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