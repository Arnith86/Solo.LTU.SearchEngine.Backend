using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class PaginationRequestParametersBuilder
{
    public static PaginationRequestParameters BuildPaginationParameters(int pageNumber, int pageSize)
    {
        return new PaginationRequestParameters{ PageNumber = pageNumber, PageSize = pageSize };     
    }   

    public static PaginationRequestParameters BuildPaginationParameters()
    {
        return new PaginationRequestParameters{ PageNumber = 1, PageSize = 10 };     
    }   
}