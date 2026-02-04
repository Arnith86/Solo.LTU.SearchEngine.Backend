using System.Net;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>
/// Represents the result of a single crawl (fetch) attempt for a URL.
/// </summary>
/// <param name="Url">The URL that was fetched during the crawl attempt.</param>
/// <param name="HTMLContent">
/// The raw HTML content returned from the HTTP response body. <br />
/// This value may be empty if the fetch failed.
/// </param>
/// <param name="ExtractedLinks">
/// A collection of URLs extracted from the fetched HTML content. <br />
/// The collection is empty if no links were found or if the page could not be parsed.
/// </param>
/// <param name="StatusCode">
/// The HTTP status code returned by the server for the crawl attempt.
/// </param>
/// <param name="TimeTakenMs">
/// The total time, in milliseconds, spent performing the HTTP request,
/// including network latency and response handling.
/// </param>
public sealed record CrawlResult(
	string Url,
	string? Title,
	string Language,
	IEnumerable<IndexedTerm> indexedTerms,
	string Type,
	byte[] Content,
	List<string> ExtractedLinks, 
	HttpStatusCode StatusCode, 
	long TimeTakenMs
);
