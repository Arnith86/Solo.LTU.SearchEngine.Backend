namespace LTU.SearchEngine.Backend.Core.Exceptions.SearchQueryExceptions;

public class QuerySyntaxException : Exception
{
	public string Title { get; }

	public QuerySyntaxException(string message, string title = "Query Syntax Violation")
		: base(message)
	{
		Title = title;
	}

	public QuerySyntaxException(
		string message,
		string query,
		string title = "Query Syntax Violation"
	) : base($"{message} in query: {query}")
	{
		Title = title;
	}
}
