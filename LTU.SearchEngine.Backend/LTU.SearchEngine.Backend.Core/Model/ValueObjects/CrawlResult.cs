using System.Net;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>
/// Represents the result of a single crawl and parse attempt for a URL.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CrawlResult"/> is a value object produced by the crawling pipeline
/// after fetching a resource and performing initial parsing and classification.
/// It contains raw content, extracted metadata, and structural information
/// required by downstream indexing and persistence steps.
/// </para>
/// </remarks>
public sealed class CrawlResult
{
	public string Url { get; }
	public string? Title { get; }
	public string Language { get; }
	public IReadOnlyCollection<IndexedTerm> IndexedTerms { get; }
	public string Type { get; }
	public byte[] Content { get; }
	public IReadOnlyList<string> ExtractedLinks { get; }
	public HttpStatusCode StatusCode { get; }
	public long TimeTakenMs { get; }

	public CrawlResult(
		string url,
		string? title,
		string language,
		IEnumerable<IndexedTerm> indexedTerms,
		string type,
		byte[] content,
		IEnumerable<string> extractedLinks,
		HttpStatusCode statusCode,
		long timeTakenMs)
	{
		Url = !string.IsNullOrWhiteSpace(url)
			? url
			: throw new ArgumentException("Url cannot be null or whitespace.", nameof(url));

		Language = !string.IsNullOrWhiteSpace(language)
			? language
			: throw new ArgumentException("Language cannot be null or whitespace.", nameof(language));

		Type = !string.IsNullOrWhiteSpace(type)
			? type
			: throw new ArgumentException("Type cannot be null or whitespace.", nameof(type));

		Content = content ?? throw new ArgumentNullException(nameof(content));

		if (indexedTerms is null)
			throw new ArgumentNullException(nameof(indexedTerms));

		if (extractedLinks is null)
			throw new ArgumentNullException(nameof(extractedLinks));

		if (timeTakenMs < 0)
			throw new ArgumentOutOfRangeException(nameof(timeTakenMs), timeTakenMs, "TimeTakenMs cannot be negative.");

		IndexedTerms = indexedTerms.ToArray();
		ExtractedLinks = extractedLinks.ToArray();
		Title = title;
		StatusCode = statusCode;
		TimeTakenMs = timeTakenMs;
	}
}
