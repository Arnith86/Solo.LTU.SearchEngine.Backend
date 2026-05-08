namespace LTU.SearchEngine.Backend.Core.Exceptions;

public class PaginationOutOfRangeException : Exception
{
    public string Title { get; }
    
    public PaginationOutOfRangeException(string message, string title = "Pagination out of range.") 
        : base (message)
    {
        Title = title;
    }
}