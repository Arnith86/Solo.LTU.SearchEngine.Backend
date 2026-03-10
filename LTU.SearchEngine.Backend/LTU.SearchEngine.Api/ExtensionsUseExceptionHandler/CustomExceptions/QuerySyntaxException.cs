namespace LTU.SearchEngine.Api.ExtensionsUseExceptionHandler.CustomExceptions;

public class QuerySyntaxException : Exception
{
	public string Title { get; }

	public QuerySyntaxException(string message, string title = "Query Syntax Violation")
		: base(message)
	{
		Title = title;
	}
}
