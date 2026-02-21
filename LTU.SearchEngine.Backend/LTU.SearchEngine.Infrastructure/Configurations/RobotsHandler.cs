using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LTU.SearchEngine.Infrastructure.Configurations
{
    public class RobotsHandler : IRobotsHandler
    {
        private readonly HttpClient _httpClient;
        private readonly CrawlerSettings _settings;

        private readonly ConcurrentDictionary<string, List<Regex>> _disallowedRulesCache = new();

        public RobotsHandler(HttpClient httpClient, CrawlerSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public bool IsAllowed(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            //Get rules for the domain
            var disallowedRules = GetRobotsRulesForDomain(uri);
            var pathAndQuery = uri.PathAndQuery;

            //controls if our URL is matching any of the Regex rules from robots.txt
            foreach(var rule in disallowedRules)
            {
                if(rule.IsMatch(pathAndQuery))
                {
                    return false; //blocked! LTU robots.txt say no!
                }
            }
            return true;
        }

        private List<Regex> GetRobotsRulesForDomain(Uri uri)
        {
            var domain = uri.Host;

            if(_disallowedRulesCache.TryGetValue(domain, out var cachedRules))
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
                            // Handles LTU:s wildcards 
                            // 1. Escape vanliga tecken (så att "." blir "\." etc.)
                            // 2. Ersätt escapad "\*" med regex-motsvarigheten ".*" (som betyder 'noll eller flera av vilket tecken som helst')
                            // 3. Lägg till "^" i början så den matchar från början av sökvägen
                            string regexPattern = "^" + Regex.Escape(path).Replace("\\*", ".*");

                            disallowedRules.Add(new Regex(regexPattern, RegexOptions.IgnoreCase));
                        }
                    }
                }
            }
            catch
            {
                // Om filen inte finns, returnera tom lista (tillåt allt)
            }

            return disallowedRules;
        }
    }
}
