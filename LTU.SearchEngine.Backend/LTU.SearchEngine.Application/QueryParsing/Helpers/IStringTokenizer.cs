using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;


/// <summary>
/// A generic interface for decomposing raw strings into a collection of categorized tokens.
/// </summary>
/// <typeparam name="TToken">The type representing the finalized token (e.g., ExtractedQueryToken).</typeparam>
/// <typeparam name="TIgnoredToken">The type representing the ignored token (e.g., IgnoredTermsDTO).</typeparam>
public interface IStringTokenizer<TToken, TIgnoredToken>
{
	/// <summary>
	/// Transforms a raw input string into a sequence of categorized tokens.
	/// </summary>
	/// <param name="input">The raw string to be tokenized.</param>
	/// <param name="languageCode">The main language for the given query.</param>
	/// <returns>
	/// A <see cref="QueryStringTokenizingResult{TToken, TIgnoredToken}"/> which contains a list of tokens of type 
	/// <typeparamref name="TToken"/> and an list of ignored tokens of type <typeparamref name="TIgnoredToken"/>.
	/// </returns>
	QueryStringTokenizingResult<TToken, TIgnoredToken> Tokenize(string input, string languageCode = "sv");
}