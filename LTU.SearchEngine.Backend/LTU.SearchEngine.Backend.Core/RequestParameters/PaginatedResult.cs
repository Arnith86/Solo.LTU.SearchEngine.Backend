namespace LTU.SearchEngine.Backend.Core.RequestParameters;


/// <summary>
/// A generic container that bundles a collection of data items with their associated pagination metadata.
/// </summary>
/// <typeparam name="T">The type of the elements contained in the result set.</typeparam>
/// <remarks>
/// This class is primarily used by repositories and services to return a subset of data (a "page") 
/// along with the information required to navigate through the entire result set.
/// </remarks>
public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; }

	/// <summary>
	/// Gets the metadata describing the pagination state (e.g., total pages, current page).
	/// </summary>
	public PaginationMetaData MetaData { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PaginatedResult{T}"/> class.
	/// </summary>
	/// <param name="items">The sequence of items belonging to the current page.</param>
	/// <param name="metaData">The pagination metadata associated with the result set.</param>
	public PaginatedResult(IEnumerable<T> items, PaginationMetaData metaData)
    {
        Items = items;
        MetaData = metaData;
    }
}