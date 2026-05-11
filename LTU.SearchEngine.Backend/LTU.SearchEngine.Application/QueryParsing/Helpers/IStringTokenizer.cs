using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;


/// <summary>
/// Defines a contract for decomposing a raw search request into a structured collection 
/// of categorized tokens and identified ignored terms.
/// </summary>
/// <remarks>
/// The tokenizer is responsible for identifying logical operators (AND, OR, NOT), 
/// quoted phrases, grouping symbols (parentheses), and handling language-specific 
/// normalization rules such as stop-word removal.
/// </remarks>
/// <typeparam name="TToken">
/// The type representing a finalized, categorized token (e.g., <see cref="ExtractedQueryToken"/>).
/// </typeparam>
/// <typeparam name="TIgnoredToken">
/// The type representing metadata for tokens that were discarded during processing, 
/// such as identified stop-words (e.g., <see cref="IgnoredTermsDTO"/>).
/// </typeparam>
public interface IStringTokenizer<TToken, TIgnoredToken>
{
	/// <summary>
	/// Transforms search request parameters into a structured <see cref="QueryStringTokenizingResult{TToken, TIgnoredToken}"/>.
	/// </summary>
	/// <param name="searchParameters">
	/// An object containing the raw input string and the global language context 
	/// required to apply correct tokenization and normalization rules.
	/// </param>
	/// <returns>
	/// A result object containing a sequence of valid tokens of type <typeparamref name="TToken"/> 
	/// and a collection of elements of type <typeparamref name="TIgnoredToken"/> that were filtered out.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="searchParameters"/> is null.</exception>
	/// <exception cref="Backend.Core.Exceptions.InvalidQueryStringException">
	/// Thrown if the input string contains invalid syntax that cannot be recovered.
	/// </exception>
	QueryStringTokenizingResult<TToken, TIgnoredToken> Tokenize(SearchQueryRequestParameters searchParameters);
}