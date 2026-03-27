using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Backend.Core.Model;

/// <summary>
/// Defines the contract for transforming raw crawl data into structured index documents.
/// </summary>
public interface IIndexingPipeline
{
    // <summary>
    /// Transforms a <see cref="CrawlResult"/> into an <see cref="IndexDocument"/> ready for storage.
    /// </summary>
    /// <param name="crawlResult">The raw result from the crawling process containing site metadata and terms.</param>
    /// <returns>A structured <see cref="IndexDocument"/> with normalized terms and calculated frequencies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="crawlResult"/> is null.</exception>
    IndexDocument Transform(CrawlResult crawlResult);
}
