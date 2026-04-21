using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public class IndexDocument
{
    public string Url { get; }
    public string Title { get; } 
    public string Language { get; }
    public DocumentMetaData MetaData { get; }
    public IEnumerable<string> OutgoingLinks { get; }
    public IReadOnlyDictionary<string, int> TitleTerms { get; }
    public IReadOnlyDictionary<string, int> HeaderTerms { get; }
    public IReadOnlyDictionary<string, int> ContentTerms { get; }
    public IReadOnlyList<string> TitleTermPositions { get; } 
    public IReadOnlyList<string> HeaderTermPositions { get; } 
    public IReadOnlyList<string> ContentTermPositions { get; } 
        
    public string ContentHash { get; }
    public DateTime LastCrawl { get; }

    public IndexDocument(
        string url, 
        string title,
        string language,
        DocumentMetaData documentMetaData,
        IEnumerable<string> outgoingLinks,
        IReadOnlyDictionary<string, int> titleTerms, 
        IReadOnlyDictionary<string, int> headerTerms, 
        IReadOnlyDictionary<string, int> contentTerms,
        IReadOnlyList<string> titleTermPositions, 
        IReadOnlyList<string> headerTermPositions, 
        IReadOnlyList<string> contentTermPositions, 
        string contentHash,
        DateTime lastCrawl
        )
    {
        Url = url;
        Title = title;
        Language = language;
        MetaData = documentMetaData;
        OutgoingLinks = outgoingLinks;
        TitleTerms = titleTerms;
        HeaderTerms = headerTerms;
        ContentTerms = contentTerms;
        TitleTermPositions = titleTermPositions;
        HeaderTermPositions = headerTermPositions;
        ContentTermPositions = contentTermPositions;
        ContentHash = contentHash;
        LastCrawl = lastCrawl; 
    }
    
    public int TotalWordCount => TitleTerms.Values.Sum() + 
                                 HeaderTerms.Values.Sum() + 
                                 ContentTerms.Values.Sum();
}

