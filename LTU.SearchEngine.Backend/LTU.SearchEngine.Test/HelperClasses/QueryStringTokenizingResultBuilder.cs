using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class QueryStringTokenizingResultBuilder
{
    public static QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO> BuildQueryStringTokenizingResult(
        IEnumerable<ExtractedQueryToken> tokens,
        IEnumerable<IgnoredTermsDTO> ignoredTokens
    )
    {
        return new QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO>(
            Tokens: tokens,
            IgnoredTokens: ignoredTokens
        );
    }
    
    public static QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO> BuildQueryStringTokenizingResult(
        IEnumerable<ExtractedQueryToken> tokens
    )
    {
        return new QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO>(
            Tokens: tokens,
            IgnoredTokens: new List<IgnoredTermsDTO>()
        );
    }
   
    public static QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO> BuildQueryStringTokenizingResult()    
    {
        return new QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO>(
            Tokens: new List<ExtractedQueryToken>(),
            IgnoredTokens: new List<IgnoredTermsDTO>()
        );
    }
}