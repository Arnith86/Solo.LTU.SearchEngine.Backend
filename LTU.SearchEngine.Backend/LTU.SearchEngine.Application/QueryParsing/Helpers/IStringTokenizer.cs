using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;


/// <summary>
/// Defines a contract for decomposing a raw input string into a structured collection 
/// of categorized tokens and identified ignored terms.
/// </summary>
/// <typeparam name="TToken">
/// The type representing a finalized, categorized token (e.g., <see cref="ExtractedQueryToken"/>).
/// </typeparam>
/// <typeparam name="TIgnoredToken">
/// The type representing metadata for tokens that were discarded during processing 
/// (e.g., IgnoredTermsDTO).
/// </typeparam>
public interface IStringTokenizer<TToken, TIgnoredToken>
{
	/// <summary>
    /// Transforms a raw input string into a structured <see cref="QueryStringTokenizingResult{TToken, TIgnoredToken}"/>.
    /// </summary>
    /// <param name="input">The raw string to be tokenized.</param>
    /// <param name="languageCode">
    /// The language context (defaulting to "sv") used to determine language-specific 
    /// tokenization rules, such as stop-words or character boundaries.
    /// </param>
    /// <returns>
    /// A result object containing a sequence of valid tokens of type <typeparamref name="TToken"/> 
    /// and a collection of ignored elements of type <typeparamref name="TIgnoredToken"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the input string is null.</exception>
	QueryStringTokenizingResult<TToken, TIgnoredToken> Tokenize(string input, string languageCode = "sv");
}