using HtmlAgilityPack;
using LTU.SearchEngine.Infrastructure.Crawler;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure
{
    public class HapHtmlParser : IHtmlParser
    {
        /// <inheritdoc/>
        public List<string> ExtractInternalLinks(string html, string baseUrl)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var internalLinks = new List<string>();

            // Try to create a Uri object from the baseUrl.
            // If the baseUrl is invalid, we cannot determine internal links, so we return an empty list.
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri baseUri))
            {
                return internalLinks; 
            }

            // Select all <a> tags that have an 'href' attribute.
            var nodes = doc.DocumentNode.SelectNodes("//a[@href]");

            // If no links are found, return the empty list immediately. 
            if (nodes == null) return internalLinks;

            foreach (var node in nodes)
            {
                // Extract the value of the href attribute.
                string href = node.GetAttributeValue("href", string.Empty);

                // Try to create a valid absolute Uri.
                // This handles combining the base URL with relative links (e.g., "/contact").
                if (Uri.TryCreate(baseUri, href, out Uri resultUri))
                {
                    // Filter criteria:
                    // 1. The scheme must be http or https (excludes mailto:, javascript:, etc).
                    // 2. The host (domain) must match the base URL's host to be considered "internal".
                    bool isHttp = resultUri.Scheme == Uri.UriSchemeHttp || resultUri.Scheme == Uri.UriSchemeHttps;
                   // bool isSameDomain = resultUri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase);

                    if (isHttp)
                    {
                        internalLinks.Add(resultUri.AbsoluteUri);
                    }

                }
            }
            // Return distinct links to avoid processing the same URL multiple times.
            return internalLinks.Distinct().ToList();
        }

        public string ExtractText(string html)
        {
            throw new NotImplementedException();
        }

        public string ExtractTitle(string html)
        {
            throw new NotImplementedException();
        }
    }
}
