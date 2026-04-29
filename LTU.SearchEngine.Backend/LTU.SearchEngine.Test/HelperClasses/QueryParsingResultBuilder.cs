using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class QueryParsingResultBuilder
{
    public static QueryParsingResult<HashSet<int>, IgnoredTermsDTO> BuildQueryParsingResult(
        QueryNode<HashSet<int>> rootNode,
        IEnumerable<IgnoredTermsDTO> ignoredTokens
    )
    {
        return new QueryParsingResult<HashSet<int>, IgnoredTermsDTO>(
            RootNode: rootNode,
            IgnoredTokens: ignoredTokens
        );
    }
    
    public static QueryParsingResult<HashSet<int>, IgnoredTermsDTO> BuildQueryParsingResult(
        QueryNode<HashSet<int>> rootNode
    )
    {
        return new QueryParsingResult<HashSet<int>, IgnoredTermsDTO>(
            RootNode: rootNode,
            IgnoredTokens: new List<IgnoredTermsDTO>()
        );
    }
   
    public static QueryParsingResult<HashSet<int>, IgnoredTermsDTO> BuildQueryParsingResult()    {
        return new QueryParsingResult<HashSet<int>, IgnoredTermsDTO>(
            RootNode: new LogicOperationNode<HashSet<int>>(
                leftNode: new TermNode<HashSet<int>>(""),
                rightNode: new TermNode<HashSet<int>>(""),
                logicalOperator: LogicalOperators.AND
            ),
            IgnoredTokens: new List<IgnoredTermsDTO>()
        );
    }
}