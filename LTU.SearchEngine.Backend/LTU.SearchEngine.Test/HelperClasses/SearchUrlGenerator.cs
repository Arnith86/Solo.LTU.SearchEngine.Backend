using Microsoft.AspNetCore.WebUtilities;

namespace LTU.SearchEngine.Test.HelperClasses;

/// <summary>
/// Dedicated utility for constructing search API URLs with query parameters.
/// </summary>
public static class SearchUrlGenerator
{
    /// <summary>
    /// Builds a URL for the search endpoint with the specified query, language, and page.
    /// </summary>
    /// <param name="query">The search string.</param>
    /// <param name="language">The ISO language code (defaults to "en").</param>
    /// <param name="pageNumber">The results page number (defaults to 1).</param>
    /// <returns>A formatted URL string with a query string attached.</returns>
    public static string QueryUrlBuilder(string query, string language = "en", int pageNumber = 1)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { "query", query },
            { "language", language },
            { "pageNumber", pageNumber.ToString() }
        };

        return QueryHelpers.AddQueryString("/api/Search", queryParams);
    }
}