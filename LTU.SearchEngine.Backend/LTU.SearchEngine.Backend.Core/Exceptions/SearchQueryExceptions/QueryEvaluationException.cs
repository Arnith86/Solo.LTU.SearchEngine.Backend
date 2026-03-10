namespace LTU.SearchEngine.Api.ExtensionsUseExceptionHandler.CustomExceptions;

public class QueryEvaluationException : Exception
{
	public string Title { get; }

	public QueryEvaluationException(string message, string title = "Query Evaluation Error")
		: base(message)
	{
		Title = title;
	}

	public QueryEvaluationException(
		string message, 
		Exception innerException, 
		string title = "Query Evaluation Error"
	) : base(message, innerException)
	{
		Title = title;
	}
}