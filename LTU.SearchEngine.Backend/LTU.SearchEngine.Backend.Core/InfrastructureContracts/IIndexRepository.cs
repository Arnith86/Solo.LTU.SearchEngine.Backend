using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.RequestParameters;


namespace LTU.SearchEngine.Infrastructure.Repositories;

/// <summary>
/// Defines the contract for the search engine's persistence layer, managing both 
/// the inverted index and document metadata.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface handle the "heavy lifting" of the search engine:
/// mapping terms to document IDs (Inverted Index) and retrieving the actual web page 
/// content for the final search results.
/// </para>
/// <para>
/// This abstraction allows the application to remain agnostic of the underlying storage 
/// (e.g., SQL, NoSQL, or In-Memory) while providing optimized methods for document lookup.
/// </para>
/// </remarks>
public interface IIndexRepository
{
	/// <summary>
	/// Persists a newly indexed document and its associated term frequencies to the storage.
	/// </summary>
	/// <param name="document">The indexed representation of a web page.</param>
	Task AddDocumentAsync(IndexDocument document);

	/// <summary>
	/// Retrieves a set of unique document IDs that contain the specified search term.
	/// </summary>
	/// <param name="term">The normalized search term to look up.</param>
	/// <returns>A unique collection of document IDs associated with the term.</returns>
	Task<HashSet<int>> GetDocumentIdsForTermAsync(string term);

	/// <summary>
	/// Retrieves a set of document IDs that contain a specific sequence of terms 
	/// (phrase) in the exact specified order.
	/// </summary>
	/// <param name="phrase">The node containing the ordered list of terms to match.</param>
	/// <returns>A unique collection of document IDs containing the exact phrase.</returns>
	Task<HashSet<int>> GetDocumentIdsForPhraseAsync(PhraseNode<HashSet<int>> phrase);

	/// <summary>
	/// Hydrates a list of document IDs into full <see cref="Page"/> entities with 
	/// support for pagination and metadata.
	/// </summary>
	/// <param name="pageIds">The list of document IDs to fetch.</param>
	/// <param name="paginationParameters">Settings for controlling page indexing and result limits.</param>
	/// <returns>A paginated result containing the requested document entities and metadata.</returns>
	Task<PaginatedResult<Page>> GetDocumentsByIdAsync(
        List<int> pageIds, PaginationRequestParameters paginationParameters);

	/// <summary>
	/// Performs a duplicate check by looking for an existing document with the same content hash.
	/// </summary>
	/// <param name="hash">The SHA-256 hash of the page content.</param>
	/// <returns>The ID of the existing document if found; otherwise, null.</returns>
	Task<int?> GetExistingDocumentByHashAsync(string hash);

	/// <summary>
	/// Updates the timestamp for when a document was last visited by the crawler.
	/// </summary>
	/// <param name="id">The unique identifier of the document.</param>
	/// <param name="newCrawl">The timestamp of the current crawl.</param>
	Task UpdateLastCrawledAsync(int id, DateTime newCrawl);
}
