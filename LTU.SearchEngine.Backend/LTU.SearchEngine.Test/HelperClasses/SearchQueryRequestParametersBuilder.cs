using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class SearchQueryRequestParametersBuilder
{
    public static SearchQueryRequestParameters BuildParameters(string query, string? language = null)
    {
        var parameters = new SearchQueryRequestParameters { Query = query };
        
        if (language != null)
        {
            parameters.Language = language;
        }
        
        return parameters;
    }

    public static SearchQueryRequestParameters BuildSearchParameters()
    {
        return BuildParameters("fakeQuery");
    }
}