using System.Text.RegularExpressions;
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
    
    /// <inheritdoc/>
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

    public string CleanRawTextForHashing(string text) => 
        Regex.Replace(text, @"\s+", " ")
            .Trim()
            .ToLowerInvariant();
        

    /// <inheritdoc/>
    public string ExtractTitle(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        //Load HTML in HtmlAgilityPack
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        //Find <title> 
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");

        //handle if tag is missing
        if (titleNode == null) return string.Empty;

        //Get text, decode HTML entities and trim whitespace
        string titleText = titleNode.InnerText;

        return HtmlEntity.DeEntitize(titleText).Trim();
    }


    public string ExtractLanguage(string html)
    {
        string languageCode = "Unknown";

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var htmlNode = doc.DocumentNode.SelectSingleNode("//html");
        
        if (htmlNode is not null)
            languageCode = htmlNode.GetAttributeValue("lang", "Unknown"); 
        
        return languageCode;
    }

    /// <inheritdoc/>
    public IEnumerable<IndexedTerm> ExtractTerms(string html)
    {
        var terms = new List<IndexedTerm>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        RemoveGarbageNodes(doc);
        HandleTitleNode(doc, terms);
        HandleHeaderNodes(doc, terms);
        HandleMetaNodes(doc, terms);
        HandleFooterNodes(doc, terms);
        HandleImageAltTextNodes(doc, terms);
        HandleBodyText(doc, terms);

        return terms;
    }
    

    private async Task<bool> IsNotRobotsBlockedAndWhitelistedAsync(string url)
    {
        if (!_domainValidator.IsWhitelisted(url))
			throw new DomainNotWhitelistedException(url);

		if (!await _robotsHandler.IsAllowedAsync(url))
			throw new BlockedByRobotsTxtException(url);   

        return true; 
    }


    // Remove non-content nodes (scripts, styles, metadata, navigation)
    private void RemoveGarbageNodes(HtmlDocument doc)
    {
        var garbageNodes = doc.DocumentNode.SelectNodes("//script|//style|//noscript|//nav");

        if (garbageNodes != null)
        {
            foreach (var node in garbageNodes) node.Remove();
        }
    }


    private void HandleTitleNode(HtmlDocument doc, List<IndexedTerm> terms)
    {   
        // We use SelectSingleNode since a valid HTML document only has one title.
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null)
        {
            AddTerms(terms, titleNode.InnerText, TermSource.Title);
            ReplaceChildWithSpaceNode(doc, titleNode);
        }
    }   
    

    private void HandleHeaderNodes(HtmlDocument doc, List<IndexedTerm> terms)
    {
        // H1-H6 tags represent section headers and are weighted higher than body text.
        var headerNodes = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6");
        if (headerNodes != null)
        {
            foreach (var node in headerNodes)
            {
                AddTerms(terms, node.InnerText, TermSource.Header);
                ReplaceChildWithSpaceNode(doc, node);
            }
        }
    }


    private void HandleMetaNodes(HtmlDocument doc, List<IndexedTerm> terms)
    {
        var whiteListedContentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "description", "keywords", "title", "abstract", "subject",
            "og:description", "og:title", "og:image:alt", "og:site_name",
            "twitter:title", "twitter:description", "twitter:image:alt",
            "citation_title", "citation_author", "citation_publication_date"
        };

        var metaNodes = doc.DocumentNode.SelectNodes("//meta[@content]");
        if (metaNodes != null)
        {
            foreach (var node in metaNodes)
            {
                // ToDo: charset should be extracted to own method and stored separately in CrawlResult
                string charset = node.GetAttributeValue("charset", "");
                if (!string.IsNullOrEmpty(charset)) AddTerms(terms, charset, TermSource.Header);
                
                string nameContent = node.GetAttributeValue("name", "");
                string propertyContent = node.GetAttributeValue("property", "");
                string content = node.GetAttributeValue("content", "");

                
                if (IsDesiredMetaContentType(whiteListedContentKeys, nameContent, propertyContent, content))
                    AddTerms(terms, content, TermSource.Header);

                ReplaceChildWithSpaceNode(doc, node);
            }
        } 
    }

    private bool IsDesiredMetaContentType(
        HashSet<string> whiteListedContentKeys,  
        string nameContent, 
        string propertyContent, 
        string content
        )
    {
        return (whiteListedContentKeys.Contains(nameContent) || 
                whiteListedContentKeys.Contains(propertyContent)) && 
                !string.IsNullOrEmpty(content);
    }

    private void HandleFooterNodes(HtmlDocument doc, List<IndexedTerm> terms)
    {
       var footerNodes = doc.DocumentNode.SelectNodes("//footer");
        if (footerNodes != null)
        {
            foreach (var node in footerNodes)
            {
                AddTerms(terms, node.InnerText, TermSource.Body);
                ReplaceChildWithSpaceNode(doc, node);
            }
        } 
    }


    private void HandleImageAltTextNodes(HtmlDocument doc, List<IndexedTerm> terms)
    {
        // --- EXTRACT IMAGE TEXT (Alt-tags) <img> with alt-attributes ---
        var imageNodes = doc.DocumentNode.SelectNodes("//img[@alt]");
        if (imageNodes != null)
        {
            foreach (var node in imageNodes)
            {
                var altText = node.GetAttributeValue("alt", "");
                
                if (!string.IsNullOrWhiteSpace(altText))
                   AddTerms(terms, altText, TermSource.Body);
                
                ReplaceChildWithSpaceNode(doc, node);
            }
        }
    }


    private void HandleBodyText(HtmlDocument doc, List<IndexedTerm> terms)
    {
        // At this stage, scripts, titles, and headers have been removed.
        // InnerText now contains only the remaining "Body" content (paragraphs, lists, divs).
        var bodyText = doc.DocumentNode.InnerText;
        AddTerms(terms, bodyText, TermSource.Body);
        
    }
    

    // Add space to prevent word concatenation after removal
    private void ReplaceChildWithSpaceNode(HtmlDocument doc, HtmlNode childNode)
    {
        if (childNode?.ParentNode is not null)
        {
            var spaceNode = doc.CreateTextNode(" "); 
            childNode.ParentNode.ReplaceChild(spaceNode, childNode); 
        }
        else
        {
            childNode?.Remove();
        }
    }


    // Helper method for Tokenization and Object Creation
    private void AddTerms(List<IndexedTerm> terms, string text, TermSource source)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var decodedText = System.Net.WebUtility.HtmlDecode(text);

        // Fix broken words split by newlines or tabs (e.g., "state-\nof-the-art" -> "state-of-the-art")
        var fixedBrokenText = Regex.Replace(decodedText, @"(?<=\w)-\s*[\r\n]+\s*", ""); 

        // This regex pattern matches words that may include letters (including accented), numbers, and certain punctuation.
        // It allows for contractions (e.g., "don't"), hyphenated words (e.g., "state-of-the-art"), and dot-separated terms (e.g., "term1.2").
        var cleanTextPattern = @"[\wåäöé]+(['.-][\wåäöé]+)*";

        var matches = Regex.Matches(fixedBrokenText, cleanTextPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            terms.Add(new IndexedTerm(match.Value.Trim(), source));
        }
    }
}

