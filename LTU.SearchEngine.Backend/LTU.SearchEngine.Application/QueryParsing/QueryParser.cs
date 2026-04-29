using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Application.QueryParsing;

public class QueryParser : IQueryParser
{
	private readonly ITreeBuilder<HashSet<int>, ExtractedQueryToken> _treeBuilder;
	private readonly IStringTokenizer<ExtractedQueryToken, IgnoredTermsDTO> _stringTokenizer;

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

	public QueryParsingResult<HashSet<int>, IgnoredTermsDTO> Parse(string rawQuery, string languageCode = "sv")
	{
		var tokenizerResult = _stringTokenizer.Tokenize(rawQuery, languageCode);

		QueryNode<HashSet<int>> rootNode = _treeBuilder.BuildTree(tokenizerResult.Tokens);

		return new QueryParsingResult<HashSet<int>, IgnoredTermsDTO>(
			rootNode, tokenizerResult.IgnoredTokens
		); 
	}
}
