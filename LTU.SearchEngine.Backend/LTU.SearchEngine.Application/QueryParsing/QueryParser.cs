using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Application.QueryParsing;

/// <summary>
/// Provides a concrete implementation of <see cref="IQueryParser"/> that orchestrates 
/// the conversion of raw text into a structured query tree.
/// </summary>
/// <remarks>
/// This implementation follows a two-step pipeline:
/// <list type="number">
/// 	<item><description>Tokenization and normalization via an <see cref="IStringTokenizer{TToken, TIgnoredToken}"/>.</description></item>
/// 	<item><description>Tree construction via an <see cref="ITreeBuilder{TResult, TToken}"/>.</description></item>
/// </list>
/// </remarks>
public class QueryParser : IQueryParser
{
	private readonly ITreeBuilder<HashSet<int>, ExtractedQueryToken> _treeBuilder;
	private readonly IStringTokenizer<ExtractedQueryToken, IgnoredTermsDTO> _stringTokenizer;

	/// <summary>
    /// Initializes a new instance of the <see cref="QueryParser"/> class.
    /// </summary>
    /// <param name="treeBuilder">The builder responsible for transforming tokens into a logical tree structure.</param>
    /// <param name="stringTokenizer">The tokenizer responsible for segmenting and normalizing the raw input string.</param>
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
	public QueryParsingResult<HashSet<int>, IgnoredTermsDTO> Parse(string rawQuery, string languageCode = "sv")
	{
		var tokenizerResult = _stringTokenizer.Tokenize(rawQuery, languageCode);

		QueryNode<HashSet<int>> rootNode = _treeBuilder.BuildTree(tokenizerResult.Tokens);

		return new QueryParsingResult<HashSet<int>, IgnoredTermsDTO>(
			rootNode, tokenizerResult.IgnoredTokens
		); 
	}
}
