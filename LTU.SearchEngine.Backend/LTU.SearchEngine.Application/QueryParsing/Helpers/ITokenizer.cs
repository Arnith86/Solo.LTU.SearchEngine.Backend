using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;


/// <summary>
/// Handles the decomposition of search strings into individual tokens or quoted phrases.
/// </summary>
public interface ITokenizer
{
	/// <summary>
	/// Processes the current buffer and adds it to the token list if valid.
	/// </summary>
	/// <param name="stringBuilder">The buffer holding characters for the current token being built.</param>
	/// <param name="tokens">The collection where the finalized token will be added.</param>
	void Flush(
		StringBuilder stringBuilder,
		bool isPhrase,
		List<ExtractedQueryToken> tokens
	);

	/// <summary>
	/// Splits input into tokens, preserving quoted substrings as single units.
	/// </summary>
	/// <param name="input">The raw search string provided by the user.</param>
	/// <returns>
	/// A list of <see cref="ExtractedQueryToken"/> representing individual terms and quoted phrases.
	/// </returns>
	List<ExtractedQueryToken> Tokenize(string input);
}