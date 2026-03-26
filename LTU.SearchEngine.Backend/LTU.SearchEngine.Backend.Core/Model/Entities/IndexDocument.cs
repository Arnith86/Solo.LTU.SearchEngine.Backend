public class IndexDocument
{
    public string Url { get; }
    public string Title { get; } 
    public IReadOnlyDictionary<string, int> TitleTerms { get; }
    public IReadOnlyDictionary<string, int> HeaderTerms { get; }
    public IReadOnlyDictionary<string, int> ContentTerms { get; }
    public string ContentHash { get; }
    public DateTime LastCrawl { get; }

    public IndexDocument(
        string url, 
        string title,
        IReadOnlyDictionary<string, int> titleTerms, 
        IReadOnlyDictionary<string, int> headerTerms, 
        IReadOnlyDictionary<string, int> contentTerms,
        string contentHash,
        DateTime lastCrawl
        )
    {
        Url = url;
        Title = title;

        TitleTerms = titleTerms;
        HeaderTerms = headerTerms;
        ContentTerms = contentTerms;
        ContentHash = contentHash;
        LastCrawl = lastCrawl;
    }
}

