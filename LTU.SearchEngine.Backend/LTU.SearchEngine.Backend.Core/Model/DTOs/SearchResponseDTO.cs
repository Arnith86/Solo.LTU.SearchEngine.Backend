using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Backend.Core.Model.DTOs;

/// <summary>
/// Represents a paginated search response containing a collection of results and execution metadata. <br/>
/// This DTO is used to deliver search data and pagination state to the client.
/// </summary>
/// <param name="searchResults">A collection of <see cref="SearchResultItem"/> matching the query for the current page.</param>
/// <param name="currentPage">The one-based index of the current result page.</param>
/// <param name="pageSize">The maximum number of items requested per page.</param>
/// <param name="totalResults">The total number of documents matching the search criteria across all pages.</param>
/// <param name="message">An optional status or warning message (e.g., "No results found" or "Query expanded").</param>
public record SearchResponseDTO(
	IEnumerable<DocumentDTO> searchResults,
	int currentPage,
	int pageSize,
	int totalResults,
	string? message
);
