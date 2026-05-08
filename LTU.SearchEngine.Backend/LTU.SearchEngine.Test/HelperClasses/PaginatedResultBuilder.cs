
using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class PaginatedResultBuilder<T>
{
    public static PaginatedResult<T> BuildPaginatedResult(List<T> items, IPaginationMetaData metaData)
    {
        return new PaginatedResult<T>(items: items, metaData: metaData);     
    }   
    
    public static PaginatedResult<T> BuildPaginatedResult(IPaginationMetaData metaData)
    {
        return new PaginatedResult<T>(items: new List<T>(), metaData: metaData);     
    }   
    
    public static PaginatedResult<T> BuildPaginatedResult(List<T> items)
    {
        return new PaginatedResult<T>(items: items, metaData: new PaginationMetaData(1, 1, 1, 1));     
    }    
}