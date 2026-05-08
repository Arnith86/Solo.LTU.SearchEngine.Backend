using LTU.SearchEngine.Backend.Core.Exceptions;

namespace LTU.SearchEngine.Backend.Core.RequestParameters;

public class PaginationRequestParameters
{
    private const int _c_maxPageSize = 100; 
    private int _pageNumber = 1;
    private int _pageSize = 10;

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
    
    public int PageSize 
    { 
        get => _pageSize;
        set => _pageSize = value > _c_maxPageSize ? _c_maxPageSize : value; 
    }

    public void Deconstruct(out int pageNumber, out int pageSize)
    {
        pageNumber = PageNumber;
        pageSize = PageSize;
    }
}
