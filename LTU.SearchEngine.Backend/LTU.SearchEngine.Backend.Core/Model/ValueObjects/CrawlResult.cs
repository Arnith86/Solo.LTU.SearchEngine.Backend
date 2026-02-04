using System.Net;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>Represents the result of a single crawl and parse attempt for a URL.</summary>
/// <remarks>
/// <para>
/// <see cref="CrawlResult"/> is a value object produced by the crawling pipeline
/// after fetching a resource and performing initial parsing and classification.
/// It contains raw content, extracted metadata, and structural information
/// required by downstream indexing and persistence steps.
/// </para>
/// </remarks>
/// <param name="Url">The absolute URL that was fetched during the crawl attempt.</param>
/// <param name="Title">
/// The extracted document title, if available. <br />
/// This value is <c>null</c> if the resource does not provide a title <br />
/// or if title extraction was not applicable.
/// </param>
/// <param name="Language">
/// The detected language of the fetched content, expressed using a standardized
/// language identifier (for example, ISO language codes).
/// </param>
/// <param name="indexedTerms">
/// A collection of terms extracted from the content together with <br />
/// their source context, used for building the search index.
/// </param>
/// <param name="Type">
/// The detected content type of the fetched resource, such as <c>text/html</c>
/// or <c>application/pdf</c>.
/// </param>
/// <param name="Content">
/// The raw response payload returned by the server. Contains the raw binary data. <br />
/// For text-based resources such as HTML and binary resources such as PDFs. 
/// </param>
/// <param name="ExtractedLinks"> 
/// A collection of absolute URLs extracted from the fetched content. <br />
/// The collection is empty if no links were found or if link extraction <br />
/// was not applicable for the resource type.
/// </param>
/// <param name="StatusCode">The HTTP status code returned by the server for the crawl attempt./// </param>
/// <param name="TimeTakenMs">
/// The total time, in milliseconds, spent performing the HTTP request, <br />
/// including network latency and response handling.
/// </param>
public sealed record CrawlResult(
	string Url,
	string? Title,
	string Language,
	IEnumerable<IndexedTerm> IndexedTerms,
	string Type,
	byte[] Content,
	List<string> ExtractedLinks, 
	HttpStatusCode StatusCode, 
	long TimeTakenMs
);
