using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;


/// <summary>
/// A generic interface for decomposing raw strings into a collection of categorized tokens.
/// </summary>
/// <typeparam name="TToken">The type representing the finalized token (e.g., ExtractedQueryToken).</typeparam>
/// <typeparam name="TType">The enum or type representing the category (e.g., QueryTokenType).</typeparam>
public interface IStringTokenizer<TToken, TType>
{
	/// <summary>
	/// Processes the current character buffer, creates a token of type <typeparamref name="TToken"/>, 
	/// and adds it to the collection.
	/// </summary>
	/// <param name="stringBuilder">The buffer holding characters for the token currently being built.</param>
	/// <param name="tokens">The collection where the finalized token will be added.</param>
	/// <param name="tokenType">The category to assign to the token (of type <typeparamref name="TType"/>).</param>
	/// <param name="languageCode">The main language for the given query.</param>
	/// <remarks>
	/// Clears the <paramref name="stringBuilder"/> after the token is added. 
	/// If the buffer is empty, no action is taken.
	/// </remarks>
	void Flush(
		StringBuilder stringBuilder,
		List<TToken> tokens,
		TType tokenType,
		string languageCode = "sv"
	);

	/// <summary>
	/// Transforms a raw input string into a sequence of categorized tokens.
	/// </summary>
	/// <param name="input">The raw string to be tokenized.</param>
	/// <param name="languageCode">The main language for the given query.</param>
	/// <returns>A list of tokens of type <typeparamref name="TToken"/>.</returns>
	List<TToken> Tokenize(string input, string languageCode = "sv");
}