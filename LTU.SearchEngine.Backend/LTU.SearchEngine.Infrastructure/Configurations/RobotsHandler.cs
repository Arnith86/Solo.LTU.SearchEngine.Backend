using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LTU.SearchEngine.Infrastructure.Configurations
{
    /// <inheritdoc/>
    public class RobotsHandler : IRobotsHandler
    {
        private readonly HttpClient _httpClient;
        private readonly CrawlerSettings _settings;
        private readonly ConcurrentDictionary<string, List<Regex>> _disallowedRulesCache = new();

        public RobotsHandler(HttpClient httpClient, CrawlerSettings settings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool IsAllowed(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (_settings.DisallowedDomains != null &&
        _settings.DisallowedDomains.Any(d => d.Equals(uri.Host, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            //Get rules for the domain
            var disallowedRules = GetRobotsRulesForDomain(uri);
            var pathAndQuery = uri.PathAndQuery;

            //controls if our URL is matching any of the Regex rules from robots.txt
            foreach (var rule in disallowedRules)
            {
                if (rule.IsMatch(pathAndQuery))
                {
                    return false; //blocked! LTU robots.txt say no!
                }
            }
            return true;
        }

        private List<Regex> GetRobotsRulesForDomain(Uri uri)
        {
            var domain = uri.Host;

            if (_disallowedRulesCache.TryGetValue(domain, out var cachedRules))
            {
                return cachedRules;
            }

            var rules = FetchAndParseRobotsTxt(uri).GetAwaiter().GetResult();
            _disallowedRulesCache[domain] = rules;
            return rules;
        }

        private async Task<List<Regex>> FetchAndParseRobotsTxt(Uri uri)
        {
            var disallowedRules = new List<Regex>();
            try
            {
                var robotsUrl = $"{uri.Scheme}://{uri.Host}/robots.txt";
                var content = await _httpClient.GetStringAsync(robotsUrl);

                var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                bool isRelevantAgent = true;

                foreach (var line in lines)
                {
                    var cleanLine = line.Trim();

                    if (cleanLine.StartsWith("#") || string.IsNullOrWhiteSpace(cleanLine)) continue;

                    if (cleanLine.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
                    {
                        var agent = cleanLine.Substring(11).Trim();
                        isRelevantAgent = (agent == "*" || agent.Equals(_settings.UserAgent, StringComparison.OrdinalIgnoreCase));
                    }
                    else if (isRelevantAgent && cleanLine.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
                    {
                        var path = cleanLine.Substring(9).Trim();
                        if (!string.IsNullOrEmpty(path))
                        {
                            // Converts the robots.txt path into a valid regex pattern:
                            // 1. "^" ensures the match starts exactly at the beginning of the URL path.
                            // 2. Regex.Escape() treats special characters (like '.') as literal text.
                            // 3. Replace() changes robots.txt wildcards ("*") into regex wildcards (".*").
                            string regexPattern = "^" + Regex.Escape(path).Replace("\\*", ".*");

                            disallowedRules.Add(new Regex(regexPattern, RegexOptions.IgnoreCase));
                        }
                    }
                }
            }
            catch
            {
                // If file not exist, return empty list (allow all)
            }

            return disallowedRules;
        }
    }
}
