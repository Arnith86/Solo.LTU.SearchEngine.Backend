using LTU.SearchEngine.Backend.Core.Model;

public class IndexDocument
{
    public string DocId { get; }
    public string Url { get; }
    public string Title { get; } 

    public Dictionary<string, int> TitleTerms { get; }
    public Dictionary<string, int> HeaderTerms { get; }
    public Dictionary<string, int> ContentTerms { get; }

    public IndexDocument(string docId, string url, string title)
    {
        DocId = docId;
        Url = url;
        Title = title;

        TitleTerms = new Dictionary<string, int>();
        HeaderTerms = new Dictionary<string, int>();
        ContentTerms = new Dictionary<string, int>();
    }
    public void AddTerm(string term, TermSource source)
    {
        var targetDictionary = source switch
        {
            TermSource.Title => TitleTerms,
            TermSource.Header => HeaderTerms,
            TermSource.Body => ContentTerms,
            _ => null
        };

        if (targetDictionary == null) return;
        targetDictionary.TryGetValue(term, out var count);
        targetDictionary[term] = count + 1;
        
    }

}

