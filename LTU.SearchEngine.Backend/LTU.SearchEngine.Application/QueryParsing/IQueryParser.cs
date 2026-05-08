using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Application.QueryParsing;

/// <summary>
/// Defines a service for transforming a raw search string into an executable query tree.
/// </summary>
/// <remarks>
/// This component orchestrates the transition from unstructured text to a structured 
/// logical representation by coordinating string tokenization and tree construction.
/// </remarks>
public interface IQueryParser
{
	/// <summary>
    /// Parses a raw query string into a structured query result, including the logical root node and any ignored terms.
    /// </summary>
    /// <param name="rawQuery">The raw, unparsed string input provided by the user.</param>
    /// <param name="languageCode">The ISO language code (e.g., "sv", "en") used to drive normalization and language-specific parsing rules.</param>
    /// <returns>
    /// A <see cref="QueryParsingResult{TResult, TIgnoredToken}"/> containing:
    /// <list type="bullet">
    /// 	<item>
    /// 		<description><see cref="QueryParsingResult{TResult, TIgnoredToken}.RootNode"/>: 
    /// 		The root of the constructed query tree used for document matching.</description>
    /// 	</item>
    /// 	<item>
    /// 		<description><see cref="QueryParsingResult{TResult, TIgnoredToken}.IgnoredTokens"/>: 
    /// 		A collection of terms that were identified but excluded (e.g., stop-words) during the tokenization process.</description>
    /// 	</item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rawQuery"/> is null.</exception>
    /// <exception cref="Backend.Core.Exceptions.InvalidQueryStringException">
    /// Thrown when the query contains syntax errors that prevent a valid tree from being constructed (e.g., mismatched grouping operators).
    /// </exception>
	// QueryParsingResult<HashSet<int>, IgnoredTermsDTO> Parse(string rawQuery, string languageCode = "sv");
	QueryParsingResult<HashSet<int>, IgnoredTermsDTO> Parse(SearchQueryRequestParameters searchParameters);
}