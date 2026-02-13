using LTU.SearchEngine.Backend.Core.Model;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

public class QuerySyntaxHelper : IQuerySyntaxHelper
{
	private readonly ITokenizer _queryTokenizer;
	public QuerySyntaxHelper(ITokenizer queryTokenizer)
	{
		_queryTokenizer = queryTokenizer ??
			throw new ArgumentNullException(nameof(queryTokenizer));
	}

	public List<string> Tokenize(string input) => 
		_queryTokenizer.Tokenize(input);

	public bool IsPhraseToken(string token) =>
		token.Length >= 2 &&
		token.StartsWith("\"", StringComparison.Ordinal) &&
		token.EndsWith("\"", StringComparison.Ordinal
	);

	public QueryMode DetectMode(List<string> tokens)
	{
		// FRQ-3004: only uppercase operators count as operators
		// FRQ-3005: whitespace implies OR by default
		if (tokens.Any(t => t is "AND" or "&&")) return QueryMode.AND;
		if (tokens.Any(t => t is "OR" or "||")) return QueryMode.OR;
		return QueryMode.OR;
	}

	public bool IsOperatorToken(string token) =>
		token is "AND" or "&&" or "OR" or "||";
}
