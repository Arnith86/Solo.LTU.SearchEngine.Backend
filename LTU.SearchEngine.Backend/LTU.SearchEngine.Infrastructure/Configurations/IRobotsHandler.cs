namespace LTU.SearchEngine.Infrastructure.Configurations;

/// <summary>
/// Provides functionality to fetch, parse, and evaluate robots.txt rules 
/// to ensure the crawler respects website policies.
/// </summary>
public interface IRobotsHandler
{
    /// <summary>
    /// Determines whether a specific URL is allowed to be crawled based on 
    /// the domain's robots.txt rules and configured exclusion lists.
    /// </summary>
    /// <param name="url">The absolute URL to check for crawling permission.</param>
    /// <returns>
    /// <c>true</c> if the URL is allowed to be crawled; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> IsAllowedAsync(string url);
}
