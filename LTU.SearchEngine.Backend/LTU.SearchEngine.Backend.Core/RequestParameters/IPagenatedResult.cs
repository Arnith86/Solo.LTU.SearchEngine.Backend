namespace LTU.SearchEngine.Backend.Core.RequestParameters;

public interface IPaginatedResult<T>
{
    IEnumerable<T> Items { get; }
    IPaginationMetaData MetaData { get; }
}
