using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public record QueryParsingResult<TResult, TIgnoredToken>(
    QueryNode<TResult> RootNode,
    IEnumerable<TIgnoredToken> IgnoredTokens
);
