using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Infrastructure.Crawling;

/// <summary>
/// Defines functionality for parsing HTML documents and extracting
/// structured information such as links, text content, and titles.
/// </summary>
public interface IHtmlParser
{
	/// <summary>
	/// Extracts all internal links from the given HTML content.
	/// </summary>
	/// <param name="html">The raw HTML content to parse.</param>
	/// <param name="baseUrl">
	/// The base URL used to resolve relative links and determine <br />
	/// whether a link is internal.
	/// </param>
	/// <returns>
	/// A list of absolute URLs representing internal links found within the HTML content.
	/// </returns>
	Task<List<string>> ExtractInternalLinks(string html, string baseUrl);

	/// <summary>
	/// Extracts the visible textual content from the given HTML, <br />	
	/// excluding markup, scripts, and styling.
	/// </summary>
	/// <param name="html">The raw HTML content to parse.</param>
	/// <returns>A plain-text representation of the visible content.</returns>
	string ExtractRawText(string html);

	/// <summary>Extracts the document title from the given HTML content.</summary>
	/// <param name="html">The raw HTML content to parse.</param>
	/// <returns>The value of the HTML <c>&lt;title&gt;</c> element, or an empty string if no title is present.</returns>
	string ExtractTitle(string html);

	/// <summary>Extracts the language code used in the given HTML content.</summary>
	/// <param name="html">The raw HTML content to parse.</param>
	/// <returns>The value of the HTML <c>&lt;html&gt;</c> element's <c>lang</c> attribute, or an "Unknown" if not specified.</returns>
	string ExtractLanguage(string html);

    /// <summary>
    /// Extracts indexable terms from raw HTML by stripping non-content elements (scripts, styles) 
    /// and tokenizing the visible text based on its structural context (Title, Header, Body).
    /// <para>
    /// <strong>Ranking Strategy (FRQ-3015):</strong> 
    /// Terms are assigned a <see cref="TermSource"/> to indicate their importance (e.g., words in &lt;h1&gt; tags 
    /// are weighted higher than body text).
    /// </para>
    /// <para>
    /// <strong>Hybrid Architecture Note:</strong> 
    /// This method performs tokenization but preserves original casing (no lowercasing). 
    /// Normalization is delegated to the Indexer (Late Normalization).
    /// </para>
    /// </summary>
    /// <param name="html">The raw HTML content to parse and tokenize.</param>
    /// <returns>
    /// A collection of <see cref="IndexedTerm"/> objects, each containing the raw token 
    /// and its structural source for ranking purposes.
    /// </returns>
    IEnumerable<IndexedTerm> ExtractTerms(string html);
}
