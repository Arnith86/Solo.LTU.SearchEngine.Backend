namespace LTU.SearchEngine.Backend.Core.RequestParameters;

/// <summary>
/// Defines the contract for pagination metadata, providing state information 
/// for navigation and result set boundaries.
/// </summary>
/// <remarks>
/// This metadata allows clients (e.g., front-end applications) to render 
/// pagination controls, such as "Next/Previous" buttons and page number indicators.
/// </remarks>
public interface IPaginationMetaData
{
	/// <summary>
	/// Gets or sets the current one-based page index.
	/// </summary>
	int CurrentPage { get; set; }

	/// <summary>
	/// Gets or sets the total number of pages available based on the <see cref="TotalItemCount"/> and <see cref="PageSize"/>.
	/// </summary>
	int TotalPages { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of items allowed per page.
	/// </summary>
	int PageSize { get; set; }

	/// <summary>
	/// Gets or sets the total number of documents matching the search criteria across all pages.
	/// </summary>
	int TotalItemCount { get; set; }

	/// <summary>
	/// Gets a value indicating whether there is at least one page available before the <see cref="CurrentPage"/>.
	/// </summary>
	/// <value><c>true</c> if <see cref="CurrentPage"/> is greater than 1; otherwise, <c>false</c>.</value>
	bool HasPrevious { get; }

	/// <summary>
	/// Gets a value indicating whether there is at least one page available after the <see cref="CurrentPage"/>.
	/// </summary>
	/// <value><c>true</c> if <see cref="CurrentPage"/> is less than <see cref="TotalPages"/>; otherwise, <c>false</c>.</value>
	bool HasNext { get; }
}