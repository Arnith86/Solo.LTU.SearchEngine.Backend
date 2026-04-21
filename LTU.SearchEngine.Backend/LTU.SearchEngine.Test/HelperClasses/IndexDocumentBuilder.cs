using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class IndexDocumentBuilder
{
    // MetaData and collections manually set 
    public static IndexDocument BuildIndexDocument(
        IEnumerable<string> outgoingLinks,
        IReadOnlyDictionary<string, int> titleTerms, 
        IReadOnlyDictionary<string, int> headerTerms, 
        IReadOnlyDictionary<string, int> contentTerms,
        IReadOnlyList<string> titleTermPositions, 
        IReadOnlyList<string> headerTermPositions, 
        IReadOnlyList<string> contentTermPositions,  
        DocumentMetaData metaData,
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
            documentMetaData: metaData,
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
   
    // MetaData with default values
    public static IndexDocument BuildIndexDocument(
        IEnumerable<string> outgoingLinks,
        IReadOnlyDictionary<string, int> titleTerms, 
        IReadOnlyDictionary<string, int> headerTerms, 
        IReadOnlyDictionary<string, int> contentTerms,
        IReadOnlyList<string> titleTermPositions, 
        IReadOnlyList<string> headerTermPositions, 
        IReadOnlyList<string> contentTermPositions,  
        bool isMetaDataPdf = false,
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
            documentMetaData: isMetaDataPdf.Equals(true)    ? 
				new PdfDocumentMetaData(pdfVersion: "v1", encodingType: "encoding")   :
				new HtmlDocumentMetaData( docType:"<!doctype html>", charSet:"utf-8"),
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
    
    // All default values
    public static IndexDocument BuildIndexDocument(
        bool isMetaDataPdf = false,
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
            documentMetaData: isMetaDataPdf.Equals(true)    ? 
				new PdfDocumentMetaData(pdfVersion: "v1", encodingType: "encoding")   :
				new HtmlDocumentMetaData( docType:"<!doctype html>", charSet:"utf-8"),
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
    
    // MetaData only manual set
    public static IndexDocument BuildIndexDocument(
        DocumentMetaData metaData,
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
            documentMetaData: metaData,
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