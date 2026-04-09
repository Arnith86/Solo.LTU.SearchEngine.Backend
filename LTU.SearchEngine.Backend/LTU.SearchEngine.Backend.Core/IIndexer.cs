using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>
/// Defines the contract for orchestrating the indexing process of crawled content.
/// </summary>
/// <remarks>
/// The indexer acts as a high-level manager that coordinates the transformation 
/// of <see cref="CrawlResult"/> data into persistent index documents. It also 
/// provides metadata operations to support incremental crawling efficiency.
/// </remarks>
public interface IIndexer
{
    /// <summary>
    /// Processes a crawl result by transforming it into an index document and persisting it.
    /// </summary>
    /// <param name="crawlResult">The result of a crawl operation containing extracted terms, links, and metadata.</param>
    /// <returns>A task representing the asynchronous indexing operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="crawlResult"/> is null.</exception>
    Task IndexAsync(CrawlResult crawlResult);
    
    /// <summary>
    /// Retrieves the unique identifier of an existing document based on its content hash.
    /// </summary>
    /// <remarks>
    /// This method is used to determine if a page's content has already been indexed, 
    /// allowing the system to skip redundant parsing and storage operations.
    /// </remarks>
    /// <param name="hash">The cryptographic hash of the page content.</param>
    /// <returns>
    /// The document ID if a match is found; otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="hash"/> is null or whitespace.</exception>
    Task<int?> GetExistingDocumentIdAsync(string hash);

    /// <summary>
    /// Updates the timestamp for when a document was last successfully crawled.
    /// </summary>
    /// <remarks>
    /// Invoked during incremental updates when the content has not changed, 
    /// ensuring the crawl schedule remains accurate without modifying the index terms.
    /// </remarks>
    /// <param name="documentId">The unique identifier of the document.</param>
    /// <param name="newCrawl">The timestamp of the current crawl attempt.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    Task UpdateIndexCrawlTimeAsync(int documentId, DateTime newCrawl);
}
