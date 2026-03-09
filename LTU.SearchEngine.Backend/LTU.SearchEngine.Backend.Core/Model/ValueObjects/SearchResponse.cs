namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public class SearchResponse
{
	public IEnumerable<SearchResultItem> SearchResults { get; }
	public int CurrentPage { get; }
	public int PageSize { get; }
	public int TotalResults { get; }
	public string Message { get; }

	public SearchResponse(
		IEnumerable<SearchResultItem> searchResults,
		int currentPage,
		int pageSize,
		int totalResults,
		string? message)
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
		IEnumerable<SearchResultItem> searchResults, 
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
