namespace LTU.SearchEngine.Backend.Core.RequestParameters;


/// <summary>
/// Provides a concrete implementation of <see cref="IPaginationMetaData"/> used to 
/// describe the state of a paginated data set.
/// </summary>
public class PaginationMetaData : IPaginationMetaData
{
	/// <inheritdoc/>
	public int CurrentPage { get; set; }
	
    /// <inheritdoc/>
	public int TotalPages { get; set; }

	/// <inheritdoc/>
	public int PageSize { get; set; }

	/// <inheritdoc/>
	public int TotalItemCount { get; set; }

	/// <inheritdoc/>
	public bool HasPrevious => CurrentPage > 1;

	/// <inheritdoc/>
	public bool HasNext => CurrentPage < TotalPages;


	/// <summary>
	/// Initializes a new instance of the <see cref="PaginationMetaData"/> class with explicit pagination values.
	/// </summary>
	/// <param name="currentPage">The current one-based page index.</param>
	/// <param name="totalPages">The total number of available pages.</param>
	/// <param name="pageSize">The number of items per page.</param>
	/// <param name="totalItemCount">The total count of items across all pages.</param>
	public PaginationMetaData(int currentPage, int totalPages, int pageSize, int totalItemCount)
    {
        CurrentPage = currentPage;
        TotalPages = totalPages;
        PageSize = pageSize;
        TotalItemCount = totalItemCount;
    }
}