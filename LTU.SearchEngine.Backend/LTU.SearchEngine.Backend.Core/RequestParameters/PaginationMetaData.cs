namespace LTU.SearchEngine.Backend.Core.RequestParameters;

public class PaginationMetaData : IPaginationMetaData
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalItemCount { get; set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public PaginationMetaData(int currentPage, int totalPages, int pageSize, int totalItemCount)
    {
        CurrentPage = currentPage;
        TotalPages = totalPages;
        PageSize = pageSize;
        TotalItemCount = totalItemCount;
    }
}