namespace LTU.SearchEngine.Test.HelperClasses;

public static class IndexDocumentBuilder
{
    public static IndexDocument BuildIndexDocument(
        IEnumerable<string> outgoingLinks,
        IReadOnlyDictionary<string, int> titleTerms, 
        IReadOnlyDictionary<string, int> headerTerms, 
        IReadOnlyDictionary<string, int> contentTerms,
        IReadOnlyList<string> titleTermPositions, 
        IReadOnlyList<string> headerTermPositions, 
        IReadOnlyList<string> contentTermPositions,  
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
            outgoingLinks: outgoingLinks,
            titleTerms: titleTerms, 
            headerTerms: headerTerms, 
            contentTerms: contentTerms,
            titleTermPositions: titleTermPositions, 
            headerTermPositions: headerTermPositions, 
            contentTermPositions: contentTermPositions,  
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
        var dummyOutgoingLinks = new List<string> { "dummyLink" };
        var dummyTitleTerms = new Dictionary<string, int> { { "titleWord", 1 } };
        var dummyHeaderTerms = new Dictionary<string, int> { { "headerWord", 1 } };
        var dummyContentTerms = new Dictionary<string, int> { { "contentWord", 1 } };
        var dummyTitleTermPositions = new List<string> { { "titleWord"} };
        var dummyHeaderTermPositions = new List<string> { { "headerWord" } };
        var dummyContentTermPositions = new List<string> { { "contentWord" } };

        return new IndexDocument(
            url: url, 
            title: title,
            language: language,
            outgoingLinks: dummyOutgoingLinks,
            titleTerms: dummyTitleTerms, 
            headerTerms: dummyHeaderTerms, 
            contentTerms: dummyContentTerms,
            titleTermPositions: dummyTitleTermPositions, 
            headerTermPositions: dummyHeaderTermPositions, 
            contentTermPositions: dummyContentTermPositions, 
            contentHash: contentHash,
            lastCrawl: tempCrawl
        );
    }
}