using LTU.SearchEngine.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LTU.SearchEngine.Infrastructure.Configurations;

/// <inheritdoc/>
public class RobotsHandler : IRobotsHandler
{
    private readonly HttpClient _httpClient;
    private readonly ICrawlerSettingsLoader _settingsLoader;
    private readonly ILogger<RobotsHandler> _logger;
    private readonly ConcurrentDictionary<string, List<Regex>> _disallowedRulesCache = new();

    public RobotsHandler(
        HttpClient httpClient,
        ICrawlerSettingsLoader settingsLoader,
        ILogger<RobotsHandler> logger
        )
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settingsLoader = settingsLoader ?? throw new ArgumentNullException(nameof(settingsLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> IsAllowedAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
       
        // Check if a rule for the domain is to be ignored
        var domain = uri.Host;
        var settings = _settingsLoader.Load();
        
        if (settings.RobotsExceptionRules != null && 
            settings.RobotsExceptionRules.TryGetValue(domain, out var exceptions))
        {
            foreach (var pattern in exceptions)
            {
                if (Regex.IsMatch(uri.PathAndQuery, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogInformation(
                        "URL '{Url}' is allowed by an exception rule for domain '{Domain}'.", 
                        url, 
                        domain
                    );
                    return true; 
                }
            }
        }

        // Get rules for the domain
        var disallowedRules = await GetRobotsRulesForDomain(uri);
        var pathAndQuery = uri.PathAndQuery;

        // Controls if our URL matches any of the Regex rules from robots.txt
        foreach (var rule in disallowedRules)
        {
            if (rule.IsMatch(pathAndQuery)) 
            {
                _logger.LogInformation(
                    "URL '{Url}' is disallowed by robots.txt rules for domain '{Domain}'.", 
                    url, 
                    domain
                );
                return false; 
            }
        }

        return true;
    }

    /// <summary>
    /// Retrieves the exclusion rules for a specific domain, using a cache to avoid redundant network requests.
    /// </summary>
    /// <param name="uri">The URI of the page being evaluated.</param>
    /// <returns>A list of compiled regular expressions representing disallowed paths.</returns>
    private async Task<List<Regex>> GetRobotsRulesForDomain(Uri uri)
    {
        var domain = uri.Host;

        if (_disallowedRulesCache.TryGetValue(domain, out var cachedRules)) return cachedRules;

        var rules = await FetchAndParseRobotsTxt(uri);
        _disallowedRulesCache[domain] = rules;

        return rules;
    }

    /// <summary>
    /// Fetches the robots.txt file from the host and parses it into a list of Regex rules
    /// targeting the configured User-Agent or the wildcard (*).
    /// </summary>
    /// <param name="uri">The URI of the domain to fetch robots.txt from.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of disallowed Regex rules.</returns>
    private async Task<List<Regex>> FetchAndParseRobotsTxt(Uri uri)
    {
        try
        {
            var robotsUrl = $"{uri.Scheme}://{uri.Host}/robots.txt";
            var content = await _httpClient.GetStringAsync(robotsUrl);
            
            return ParseRobotsTxt(content);
        }
        catch
        {
            return new List<Regex>(); // If file not exist, return empty list (allow all)
        }
    }

    /// <summary>
    /// If specific rules for current userAgent exist use them, otherwise use wildcard rules. 
    /// </summary>
    private List<Regex> ParseRobotsTxt(string content)
    {
        var specificRules = new List<Regex>();
        var wildcardRules = new List<Regex>();

        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        string currentAgent = "";

        foreach (var line in lines)
        {
            var cleanLine = line.Trim();

            // Removes comments and empty rows
            if (cleanLine.StartsWith("#") || string.IsNullOrWhiteSpace(cleanLine)) continue;


            if (cleanLine.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
            {
                currentAgent = cleanLine.Substring(11).Trim();
                continue;
            }
            
            if (cleanLine.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
            {
                var path = cleanLine.Substring(9).Trim();

                if (string.IsNullOrEmpty(path)) continue;
                var regex = PathToRegex(path);

                if (currentAgent.Equals("*")) 
                    wildcardRules.Add(regex);
                else if (currentAgent.Equals(_settingsLoader.Load().UserAgent, StringComparison.OrdinalIgnoreCase))
                    specificRules.Add(regex);

            }
        }
        
        return specificRules.Any() ? specificRules : wildcardRules;
    }

    private Regex PathToRegex(string path)
    {
        // Converts the robots.txt path into a valid regex pattern:
        // 1. "^" ensures the match starts exactly at the beginning of the URL path.
        // 2. Regex.Escape() treats special characters (like '.') as literal text.
        // 3. Replace() changes robots.txt wildcards ("*") into regex wildcards (".*").
        return new Regex("^" + Regex.Escape(path).Replace("\\*", ".*"), RegexOptions.IgnoreCase);
    }
}

