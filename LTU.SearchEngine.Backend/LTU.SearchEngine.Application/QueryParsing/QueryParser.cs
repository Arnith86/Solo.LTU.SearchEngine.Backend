using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.RequestParameters;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Application.QueryParsing;

/// <summary>
/// Provides a concrete implementation of <see cref="IQueryParser"/> that orchestrates 
/// the conversion of raw search parameters into a structured, executable query tree.
/// </summary>
/// <remarks>
/// This implementation executes a two-stage processing pipeline:
/// <list type="number">
///     <item>
///         <term>Tokenization</term>
///         <description>
///             The raw input string is decomposed into categorized tokens (terms, phrases, operators) 
///             and normalized according to the specified language context.
///         </description>
///     </item>
///     <item>
///         <term>Tree Construction</term>
///         <description>
///             The resulting token stream is parsed into a hierarchical tree of <see cref="QueryNode{T}"/> 
///             objects, representing the final boolean logic.
///         </description>
///     </item>
/// </list>
/// </remarks>
public class QueryParser : IQueryParser
{
	private readonly ITreeBuilder<HashSet<int>, ExtractedQueryToken> _treeBuilder;
	private readonly IStringTokenizer<ExtractedQueryToken, IgnoredTermsDTO> _stringTokenizer;

	/// <summary>
	/// Initializes a new instance of the <see cref="QueryParser"/> class with required dependencies.
	/// </summary>
	/// <param name="treeBuilder">The builder responsible for transforming normalized tokens into a logical tree structure.</param>
	/// <param name="stringTokenizer">The tokenizer responsible for segmenting and cleaning the raw input string.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="treeBuilder"/> or <paramref name="stringTokenizer"/> is null.</exception>
	public QueryParser(
		ITreeBuilder<HashSet<int>, ExtractedQueryToken> treeBuilder,
		IStringTokenizer<ExtractedQueryToken, IgnoredTermsDTO> stringTokenizer
		)
	{
		_treeBuilder = treeBuilder ??
			throw new ArgumentNullException("Tree builder cannot be null.", nameof(treeBuilder));

		_stringTokenizer = stringTokenizer ??
			throw new ArgumentNullException("String tokenizer cannot be null.", nameof(stringTokenizer));
	}

	/// <inheritdoc/>
	public QueryParsingResult<HashSet<int>, IgnoredTermsDTO> Parse(SearchQueryRequestParameters searchParameters)
	{
		var tokenizerResult = _stringTokenizer.Tokenize(searchParameters);

		QueryNode<HashSet<int>> rootNode = _treeBuilder.BuildTree(tokenizerResult.Tokens);

		return new QueryParsingResult<HashSet<int>, IgnoredTermsDTO>(
			rootNode, tokenizerResult.IgnoredTokens
		); 
	}
}
