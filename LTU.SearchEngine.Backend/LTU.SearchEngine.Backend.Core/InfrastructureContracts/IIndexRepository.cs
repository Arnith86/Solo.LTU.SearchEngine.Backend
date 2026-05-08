using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.RequestParameters;


namespace LTU.SearchEngine.Infrastructure.Repositories;

/// <summary>
/// Defines the contract for storing and retrieving indexed documents and terms.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface are responsible for persisting index data,
/// including forward indexes and inverted indexes.
/// </para>
/// <para>
/// The repository abstraction hides storage details from the indexing flow and
/// allows different persistence strategies to be introduced without changing
/// the indexing logic.
/// </para>
/// </remarks>
public interface IIndexRepository
{
    Task AddDocumentAsync(IndexDocument document);
	Task<HashSet<int>> GetDocumentIdsForTermAsync(string term);
	Task<HashSet<int>> GetDocumentIdsForPhraseAsync(PhraseNode<HashSet<int>> phrase); 
	// Task<List<Page>> GetDocumentsByIdAsync(List<int> pageIds);
	Task<PaginatedResult<Page>> GetDocumentsByIdAsync(
        List<int> pageIds, 
        PaginationRequestParameters paginationParameters
    );
	Task<int?> GetExistingDocumentByHashAsync(string hash);
	Task UpdateLastCrawledAsync(int id, DateTime newCrawl);
}
