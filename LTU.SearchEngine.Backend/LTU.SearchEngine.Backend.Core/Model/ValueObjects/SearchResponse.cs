using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model.DTOs;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>
/// Represents a complete search result response, including the result items and pagination metadata.
/// </summary>
public class SearchResponse
{
	/// <summary>Gets the collection of search result items.</summary>
	public IEnumerable<Page> SearchResults { get; }

	/// <summary>Gets the current page number in the paginated result set.</summary>
	public int CurrentPage { get; }

	/// <summary>Gets the number of items per page.</summary>
	public int PageSize { get; }

	/// <summary>Gets the total number of results found across all pages.</summary>
	public int TotalResults { get; }

	/// <summary>Gets an optional status or error message related to the search operation.</summary>
	public string Message { get; }


	/// <summary>Initializes a new instance of the <see cref="SearchResponse"/> class.</summary>
	/// <param name="searchResults">A collection of search results. Cannot be null.</param>
	/// <param name="currentPage">The current page index (1-based).</param>
	/// <param name="pageSize">The maximum number of items per page.</param>
	/// <param name="totalResults">The total count of results available.</param>
	/// <param name="message">An optional message, such as success or error details.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="searchResults"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if numeric values are outside valid ranges.</exception>
	public SearchResponse(
		IEnumerable<Page> searchResults,
		int currentPage,
		int pageSize,
		int totalResults,
		string? message
		)
	{
		ValidateAttributes(
			searchResults, currentPage, pageSize, totalResults, message
		);

		SearchResults = searchResults;
		CurrentPage = currentPage;
		PageSize = pageSize;
		TotalResults = totalResults;
		Message = message!;
	}

	private void ValidateAttributes(
		IEnumerable<Page> searchResults, 
		int currentPage, 
		int pageSize, 
		int totalResults,
		string? message
	)
	{
		ArgumentNullException.ThrowIfNull(searchResults);
		ValidatePagination(currentPage, pageSize, totalResults);
	}

	private void ValidatePagination(
		int currentPage, 
		int pageSize, 
		int totalResults
	)
	{
		if (currentPage < 1) 
			throw new ArgumentOutOfRangeException(
				nameof(currentPage), 
				"Page must have a minimum value of 1."
			);

		if (pageSize < 0) 
			throw new ArgumentOutOfRangeException(
				nameof(pageSize), 
				"Page size cannot be a negative value."
			);
		
		if (totalResults < 0) 
			throw new ArgumentOutOfRangeException(
				nameof(totalResults), 
				"Total results cannot have a negative value."
			);
	}
}
