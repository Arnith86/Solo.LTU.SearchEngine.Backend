namespace LTU.SearchEngine.Application.QueryParsing.Helpers;


/// <summary>
/// A generic interface for decomposing raw strings into a collection of categorized tokens.
/// </summary>
/// <typeparam name="TToken">The type representing the finalized token (e.g., ExtractedQueryToken).</typeparam>
public interface IStringTokenizer<TToken, TType>
{
	/// <summary>
	/// Transforms a raw input string into a sequence of categorized tokens.
	/// </summary>
	/// <param name="input">The raw string to be tokenized.</param>
	/// <param name="languageCode">The main language for the given query.</param>
	/// <returns>A list of tokens of type <typeparamref name="TToken"/>.</returns>
	List<TToken> Tokenize(string input, string languageCode = "sv");
}