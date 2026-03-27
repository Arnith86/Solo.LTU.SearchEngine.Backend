using System.Net;
using System.Text;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.HelperClasses;

public class CrawlResultBuilder
{
    public static CrawlResult BuildCrawlResult(
        string url = "https://example.com",
		string title = "x",
	    string language = "en",
		string type = "text/html",
		string content = "x",
		HttpStatusCode statusCode = HttpStatusCode.OK,
		int timeTakenMs = 1,
		string hashContent = "FakeHash",
		DateTime dateTime = default
        )
    {
		var crawledAt = dateTime == default ? DateTime.UtcNow : dateTime; 

        return new CrawlResult(
			url: url,
			title: title,
			language: language,
			indexedTerms: new List<IndexedTerm>(),
			type: type,
			content: Encoding.UTF8.GetBytes(content),
			extractedLinks: Array.Empty<string>(),
			statusCode: statusCode,
			timeTakenMs: timeTakenMs,
            contentHash: hashContent,
			crawledAt: crawledAt
		);
    }

    public static CrawlResult BuildCrawlResult(
        IEnumerable<IndexedTerm> indexedTerms,
        IEnumerable<string> extractedLinks,
        string url = "https://example.com",
		string title = "x",
	    string language = "en",
		string type = "text/html",
		string content = "x",
		HttpStatusCode statusCode = HttpStatusCode.OK,
		int timeTakenMs = 1,
		string hashContent = "FakeHash",
		DateTime dateTime = default
        )
    {
		var crawledAt = dateTime == default ? DateTime.UtcNow : dateTime; 

        return new CrawlResult(
			url: url,
			title: title,
			language: language,
			indexedTerms: indexedTerms!,
			type: type,
			content: Encoding.UTF8.GetBytes(content),
			extractedLinks: extractedLinks!,
			statusCode: statusCode,
			timeTakenMs: timeTakenMs,
            contentHash: hashContent,
			crawledAt: crawledAt
		);
    }
}