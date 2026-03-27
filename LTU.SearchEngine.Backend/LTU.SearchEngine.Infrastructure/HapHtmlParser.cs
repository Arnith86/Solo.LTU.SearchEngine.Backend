using HtmlAgilityPack;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Configurations;
using LTU.SearchEngine.Infrastructure.Crawling;
using Microsoft.Extensions.Logging;

namespace LTU.SearchEngine.Infrastructure;

public class HapHtmlParser : IHtmlParser
{
    private readonly IDomainValidator _domainValidator;
    private readonly IRobotsHandler _robotsHandler;
    private readonly ILogger _logger;

    public HapHtmlParser(
        IDomainValidator domainValidator, 
        IRobotsHandler robotsHandler,
        ILogger<HapHtmlParser> logger
        )
    {
        _domainValidator = domainValidator ?? throw new ArgumentNullException(nameof(domainValidator)); 
        _robotsHandler = robotsHandler ?? throw new ArgumentNullException(nameof(robotsHandler)); 
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<List<string>> ExtractInternalLinks(string html, string baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var internalLinks = new List<string>();

        // Try to create a Uri object from the baseUrl.
        // If the baseUrl is invalid, we cannot determine internal links, so we return an empty list.
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri))
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
            if (Uri.TryCreate(baseUri, href, out Uri? resultUri))
            {
                // Filter criteria:
                // 1. The scheme must be http or https (excludes mailto:, javascript:, etc).
                // 2. The host (domain) must match the base URL's host to be considered "internal".
                bool isHttp = resultUri.Scheme == Uri.UriSchemeHttp || resultUri.Scheme == Uri.UriSchemeHttps;
                
                if (isHttp)
                {
                    string url = resultUri.AbsoluteUri;

                    try
                    {
                        await IsNotRobotsBlockedAndWhitelistedAsync(url);
                        internalLinks.Add(url);
                    }
                    catch (DomainNotWhitelistedException ex)
                    {
                        _logger.LogWarning($"Url: {url} skipped: domain not whitelisted ({ex.Message})");
                    }
                    catch (BlockedByRobotsTxtException ex)
                    {
                        _logger.LogWarning($"Job {url} skipped: URL blocked by robots.txt ({ex.Message})");
                    }
                }
            }
        }
        // Return distinct links to avoid processing the same URL multiple times.
        return internalLinks.Distinct().ToList();
    }
    

    private async Task<bool> IsNotRobotsBlockedAndWhitelistedAsync(string url)
    {
        if (!_domainValidator.IsWhitelisted(url))
			throw new DomainNotWhitelistedException(url);

		if (!await _robotsHandler.IsAllowedAsync(url))
			throw new BlockedByRobotsTxtException(url);   

        return true; 
    }


    /// <inheritdoc/>
    public IEnumerable<IndexedTerm> ExtractTerms(string html)
    {
        var terms = new List<IndexedTerm>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // --- 1. CLEANUP
        // Remove non-content nodes (scripts, styles, metadata, navigation) 
        // to prevent indexing code or irrelevant UI elements.
        var garbageNodes = doc.DocumentNode.SelectNodes("//script|//style|//noscript|//nav");

        if (garbageNodes != null)
        {
            foreach (var node in garbageNodes)  node.Remove();
            
        }

        // --- 2. EXTRACT TITLE (High Ranking Priority) ---
        // The <title> tag contains the most relevant keywords for the page
        // We use SelectSingleNode since a valid HTML document only has one title.
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null)
        {
            AddTerms(terms, titleNode.InnerText, TermSource.Title);
            titleNode.Remove(); 
        }

        // --- 3. EXTRACT HEADERS (Medium Ranking Priority) ---
        // H1-H6 tags represent section headers and are weighted higher than body text.
        var headerNodes = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6");
        if (headerNodes != null)
        {
            foreach (var node in headerNodes)
            {
                AddTerms(terms, node.InnerText, TermSource.Header);

                node.Remove();
            }
        }

        // --- 3. EXTRACT META DATA (Fix for UTF-8 test) ---
        var metaNodes = doc.DocumentNode.SelectNodes("//meta");
        if (metaNodes != null)
        {
            foreach (var node in metaNodes)
            {
                // Meta-tags seldomly have meaningful InnerText, so we check attributes like 'charset' or 'content'
                var content = node.GetAttributeValue("content", "");
                var charset = node.GetAttributeValue("charset", "");

                if (!string.IsNullOrEmpty(content)) AddTerms(terms, content, TermSource.Header);
                if (!string.IsNullOrEmpty(charset)) AddTerms(terms, charset, TermSource.Header);

                node.Remove();
            }
        }

        var footerNodes = doc.DocumentNode.SelectNodes("//footer");
        if (footerNodes != null)
        {
            foreach (var node in footerNodes)
            {
                AddTerms(terms, node.InnerText, TermSource.Body);

                node.Remove();
            }
        }

        // --- EXTRACT IMAGE TEXT (Alt-tags) <img> with alt-attributes ---
        var imageNodes = doc.DocumentNode.SelectNodes("//img[@alt]");
        if (imageNodes != null)
        {
            foreach (var node in imageNodes)
            {
                var altText = node.GetAttributeValue("alt", "");
                if (!string.IsNullOrWhiteSpace(altText))
                {
                    // alt-text is often given the same importance as a body or header
                    AddTerms(terms, altText, TermSource.Body);
                }
                node.Remove();
            }
        }

        // --- 4. EXTRACT BODY TEXT (Low/Standard Ranking Priority) ---
        // At this stage, scripts, titles, and headers have been removed.
        // InnerText now contains only the remaining "Body" content (paragraphs, lists, divs).
        var bodyText = doc.DocumentNode.InnerText;
        AddTerms(terms, bodyText, TermSource.Body);

        return terms;
    }

    // Helper method for Tokenization and Object Creation
    private void AddTerms(List<IndexedTerm> terms, string text, TermSource source)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var decodedText = System.Net.WebUtility.HtmlDecode(text);

        var words = decodedText.Split(
            new[] { ' ', '\r', '\n', '\t' },
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach (var word in words)
        {
            terms.Add(new IndexedTerm(word.Trim(), source));
        }
    }

    public string ExtractRawText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove garbage
        var garbageNodes = doc.DocumentNode.SelectNodes("//script|//style|//noscript|//nav|//footer");
        if(garbageNodes != null)
        {
            foreach (var node in garbageNodes) node.Remove();
        }

        // Return all text as a single string
        string plainText = doc.DocumentNode.InnerText;
        return HtmlEntity.DeEntitize(plainText).Trim();
    }

    public string ExtractTitle(string html)
    {
        if(string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        //Load HTML in HtmlAgilityPack
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        //Find <title> 
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");

        //handle if tag is missing
        if(titleNode == null)
        {
            return string.Empty;
        }

        //Get text, decode HTML entities and trim whitespace
        string titleText = titleNode.InnerText;

        return HtmlEntity.DeEntitize(titleText).Trim();
    }
}

