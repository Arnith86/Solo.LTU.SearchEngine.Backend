namespace LTU.SearchEngine.Backend.Core.RequestParameters;

public interface IPaginationMetaData
{
    int CurrentPage { get; set; }
    int TotalPages { get; set; }
    int PageSize { get; set; }
    int TotalItemCount { get; set; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}