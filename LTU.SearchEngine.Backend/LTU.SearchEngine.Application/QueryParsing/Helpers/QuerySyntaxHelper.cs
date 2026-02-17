using LTU.SearchEngine.Backend.Core.Enums;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <inheritdoc />
public class QuerySyntaxHelper : IQuerySyntaxHelper
{
	private readonly ITokenizer _queryTokenizer;

	public QuerySyntaxHelper(ITokenizer queryTokenizer)
	{
		_queryTokenizer = queryTokenizer ??
			throw new ArgumentNullException(nameof(queryTokenizer));
	}

	/// <inheritdoc />
	public List<string> Tokenize(string input) => 
		_queryTokenizer.Tokenize(input);

	/// <inheritdoc />
	public bool IsPhraseToken(string token) =>
		token.Length >= 2 &&
		token.StartsWith("\"", StringComparison.Ordinal) &&
		token.EndsWith("\"", StringComparison.Ordinal
	);


	// ToDo: When we expand to support more complex queries, this method will be updated or removed.
	/// <inheritdoc />
	public QueryMode DetectMode(List<string> tokens)
	{
		if (tokens.Any(t => t is "AND" or "&&")) return QueryMode.AND;
		else if (tokens.Any(t => t is "OR" or "||")) return QueryMode.OR;
		return QueryMode.OR;
	}

	/// <inheritdoc />
	public bool IsOperatorToken(string token) =>
		token is "AND" or "&&" or "OR" or "||";
}
