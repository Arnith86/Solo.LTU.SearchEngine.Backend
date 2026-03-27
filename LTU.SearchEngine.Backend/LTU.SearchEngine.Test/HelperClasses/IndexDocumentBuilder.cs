namespace LTU.SearchEngine.Test.HelperClasses;

public static class IndexDocumentBuilder
{
    public static IndexDocument BuildIndexDocument(
        IReadOnlyDictionary<string, int> titleTerms, 
        IReadOnlyDictionary<string, int> headerTerms, 
        IReadOnlyDictionary<string, int> contentTerms,
        string url = "http://test.html", 
        string title = "Test Title",
        string language = "sv",
        string contentHash = "x",
        DateTime lastCrawl = default
        )
    {
        var tempCrawl = lastCrawl == default ? DateTime.UtcNow : lastCrawl;

        return new IndexDocument(
            url: url, 
            title: title,
            language: language,
            titleTerms: titleTerms, 
            headerTerms: headerTerms, 
            contentTerms: contentTerms,
            contentHash: contentHash,
            lastCrawl: tempCrawl
        );
    }
    
    public static IndexDocument BuildIndexDocument(
        string url = "http://test.html", 
        string title = "Test Title",
        string language = "sv",
        string contentHash = "x",
        DateTime lastCrawl = default
        )
    {
        var tempCrawl = lastCrawl == default ? DateTime.UtcNow : lastCrawl;

        var dummyTitleTerms = new Dictionary<string, int> { { "titleWord", 1 } };
        var dummyHeaderTerms = new Dictionary<string, int> { { "headerWord", 1 } };
        var dummyContentTerms = new Dictionary<string, int> { { "contentWord", 1 } };

        return new IndexDocument(
            url: url, 
            title: title,
            language: language,
            titleTerms: dummyTitleTerms, 
            headerTerms: dummyHeaderTerms, 
            contentTerms: dummyContentTerms,
            contentHash: contentHash,
            lastCrawl: tempCrawl
        );
    }
}