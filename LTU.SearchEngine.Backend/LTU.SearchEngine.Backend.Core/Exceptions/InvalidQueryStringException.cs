namespace LTU.SearchEngine.Backend.Core.Exceptions;

public class InvalidQueryStringException : FormatException
{
	public string Query { get; }

	public InvalidQueryStringException(string message, string query) 
		: base($"{message} in query: {query}")
	{
		Query = query;
	}
}
