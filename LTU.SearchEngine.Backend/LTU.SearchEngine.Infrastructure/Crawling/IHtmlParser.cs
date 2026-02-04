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
	List<string> ExtractInternalLinks(string html, string baseUrl);

	/// <summary>
	/// Extracts the visible textual content from the given HTML, <br />	
	/// excluding markup, scripts, and styling.
	/// </summary>
	/// <param name="html">The raw HTML content to parse.</param>
	/// <returns>A plain-text representation of the visible content.</returns>
	string ExtractText(string html);

	/// <summary>
	/// Extracts the document title from the given HTML content.
	/// </summary>
	/// <param name="html">The raw HTML content to parse.</param>
	/// <returns>
	/// The value of the HTML <c>&lt;title&gt;</c> element, or an empty <br />
	/// string if no title is present.
	/// </returns>
	string ExtractTitle(string html); 
}
