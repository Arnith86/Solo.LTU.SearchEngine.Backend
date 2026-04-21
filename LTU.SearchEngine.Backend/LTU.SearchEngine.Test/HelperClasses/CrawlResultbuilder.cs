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
			metaData: type.Equals("text/html") ? 
				new HtmlDocumentMetaData("<!doctype html>", "utf-8") : 
				new PdfDocumentMetaData("v1", "encoding"),
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
			metaData: type.Equals("text/html") ? 
				new HtmlDocumentMetaData("<!doctype html>", "utf-8") : 
				new PdfDocumentMetaData("v1", "encoding"),
			indexedTerms: indexedTerms,
			type: type,
			content: Encoding.UTF8.GetBytes(content),
			extractedLinks: extractedLinks!,
			statusCode: statusCode,
			timeTakenMs: timeTakenMs,
            contentHash: hashContent,
			crawledAt: crawledAt
		);
    }

	public static ProcessJobResponse ProcessJobResponseBuilder(
		bool changedContent, 
		DateTime processedAt, 
		CrawlResult? crawlResult
		)
	{
		return new ProcessJobResponse(
			ChangedContent: changedContent,
			ProcessedAt: processedAt, 
			CrawlResult: crawlResult
		);
	}
}