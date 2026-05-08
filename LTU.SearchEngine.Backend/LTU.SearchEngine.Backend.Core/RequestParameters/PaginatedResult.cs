namespace LTU.SearchEngine.Backend.Core.RequestParameters;

public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; }
    public PaginationMetaData MetaData { get; }

    public PaginatedResult(IEnumerable<T> items, PaginationMetaData metaData)
    {
        Items = items;
        MetaData = metaData;
    }
}