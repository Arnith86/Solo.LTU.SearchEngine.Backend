using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Application.QueryParsing;

/// <summary>
/// Defines a service for transforming raw search parameters into an executable query tree.
/// </summary>
/// <remarks>
/// This component orchestrates the transition from unstructured text to a structured 
/// logical representation. It coordinates the workflow between string tokenization 
/// and tree construction to produce a result ready for database execution.
/// </remarks>
public interface IQueryParser
{
	/// <summary>
	/// Parses search request parameters into a structured query result, including 
	/// the logical root node and any ignored terms.
	/// </summary>
	/// <param name="searchParameters">
	/// An object containing the raw query string and global language context 
	/// provided by the user.
	/// </param>
	/// <returns>
	/// A <see cref="QueryParsingResult{HashSet, IgnoredTermsDTO}"/> containing:
	/// <list type="bullet">
	///     <item>
	///         <term>RootNode</term>
	///         <description>The root of the constructed query tree used for document matching and filtering.</description>
	///     </item>
	///     <item>
	///         <term>IgnoredTokens</term>
	///         <description>A collection of terms (e.g., stop-words) that were identified but excluded from the final tree.</description>
	///     </item>
	/// </list>
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="searchParameters"/> is null.</exception>
	/// <exception cref="LTU.SearchEngine.Backend.Core.Exceptions.InvalidQueryStringException">
	/// Thrown when the query contains syntax errors that prevent a valid tree from being constructed, 
	/// such as mismatched parentheses or invalid operator placement.
	/// </exception>
	QueryParsingResult<HashSet<int>, IgnoredTermsDTO> Parse(SearchQueryRequestParameters searchParameters);
}