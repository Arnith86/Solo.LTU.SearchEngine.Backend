using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// Provides syntax analysis and tokenization utilities for search queries.
/// </summary>
public interface IQuerySyntaxHelper
{
	/// <summary>
	/// Invokes the underlying tokenizer to split input into parts.
	/// </summary>
	List<ExtractedQueryToken> Tokenize(string input);

	/// <summary>
	/// Detects the QueryMode (AND/OR) based on presence of operators.
	/// </summary>
	QueryMode DetectMode(List<string> tokens);

	/// <summary>
	/// Identifies if a token is a valid logical operator.
	/// </summary>
	bool IsOperatorToken(string token);

	///// <summary> 
	///// Checks if the token is wrapped in double quotes.
	///// </summary>
	//bool IsPhraseToken(string token);
}