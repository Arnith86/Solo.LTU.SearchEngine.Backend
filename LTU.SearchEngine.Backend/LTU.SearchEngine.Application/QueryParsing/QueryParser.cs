using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Application.QueryParsing;

public class QueryParser : IQueryParser
{
	private readonly ITreeBuilder<HashSet<int>, ExtractedQueryToken> _treeBuilder;
	private readonly IStringTokenizer<ExtractedQueryToken, QueryTokenType> _stringTokenizer;

	public QueryParser(
		ITreeBuilder<HashSet<int>, ExtractedQueryToken> treeBuilder,
		IStringTokenizer<ExtractedQueryToken, QueryTokenType> stringTokenizer
		)
	{
		_treeBuilder = treeBuilder ??
			throw new ArgumentNullException("Tree builder cannot be null.", nameof(treeBuilder));

		_stringTokenizer = stringTokenizer ??
			throw new ArgumentNullException("String tokenizer cannot be null.", nameof(stringTokenizer));
	}

	public QueryNode<HashSet<int>> Parse(string rawQuery)
	{
		List<ExtractedQueryToken> tokens = _stringTokenizer.Tokenize(rawQuery);
		QueryNode<HashSet<int>> rootNode = _treeBuilder.BuildTree(tokens);

		return rootNode;
	}
}
