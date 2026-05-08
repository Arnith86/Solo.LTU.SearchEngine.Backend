namespace LTU.SearchEngine.Backend.Core.RequestParameters;

public class PaginatedResult<T> : IPaginatedResult<T>
{
    public IEnumerable<T> Items { get; }
    public IPaginationMetaData MetaData { get; }

    public PaginatedResult(IEnumerable<T> items, IPaginationMetaData metaData)
    {
        Items = items;
        MetaData = metaData;
    }
}