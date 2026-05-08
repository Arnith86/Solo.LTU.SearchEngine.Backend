using LTU.SearchEngine.Backend.Core.Exceptions;

namespace LTU.SearchEngine.Backend.Core.RequestParameters;


/// <summary>
/// Encapsulates request parameters for controlling pagination, including validation logic and constraints.
/// </summary>
/// <remarks>
/// This class enforces engine-level restrictions to ensure performance and prevent excessive 
/// resource consumption, such as limiting the maximum page size and restricting deep pagination depth.
/// </remarks>
public class PaginationRequestParameters
{
    private const int _c_maxPageSize = 100; 
    private int _pageNumber = 1;
    private int _pageSize = 10;

	/// <summary>
	/// Gets or sets the requested page number.
	/// </summary>
	/// <value>Defaults to 1. Minimum value is 1.</value>
	/// <exception cref="PaginationOutOfRangeException">
	/// Thrown if the value exceeds the deep pagination limit (currently 100) to protect system performance.
	/// </exception>
	public int PageNumber 
    { 
        get => _pageNumber;
        set
        {
            if (value > 100)
            {
                throw new PaginationOutOfRangeException(
                    "Deep pagination is restricted. Please refine your query!"
                );
            }
            
            _pageNumber = value < 1 ? 1: value;
        }
    }

	/// <summary>
	/// Gets or sets the number of items per page.
	/// </summary>
	/// <value>Defaults to 10. Automatically capped at the maximum allowed size (currently 100).</value>
	public int PageSize 
    { 
        get => _pageSize;
        set => _pageSize = value > _c_maxPageSize ? _c_maxPageSize : value; 
    }


	/// <summary>
	/// Allows the object to be deconstructed into its constituent page number and page size components.
	/// </summary>
	/// <param name="pageNumber">The validated current page number.</param>
	/// <param name="pageSize">The validated current page size.</param>
	public void Deconstruct(out int pageNumber, out int pageSize)
    {
        pageNumber = PageNumber;
        pageSize = PageSize;
    }
}
