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

    /// <summary>
    /// Adds a term to the document and updates its Term Frequency (TF).
    /// </summary>
    /// <param name="term">
    /// The normalized term to add. 
    /// Must not be null, empty, or whitespace.
    /// </param>
    /// <param name="source">
    /// Indicates where the term was found (Title, Header, or Body).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="term"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="term"/> is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="source"/> is not a valid <see cref="TermSource"/> value.
    /// </exception>
    /// <remarks>
    /// This method:
    /// - Separates terms based on their source (Title, Header, Body).
    /// - Increments frequency if the term already exists.
    /// - Ensures the document maintains a valid and consistent indexing state.
    /// </remarks>
    public void AddTerm(string term, TermSource source)
    {
        if (term == null) throw new ArgumentNullException(nameof(term));
        if (string.IsNullOrWhiteSpace(term))
            throw new ArgumentException("Term cannot be empty or whitespace.", nameof(term));


        var targetDictionary = source switch
        {
            TermSource.Title => TitleTerms,
            TermSource.Header => HeaderTerms,
            TermSource.Body => ContentTerms,
            _ => throw new ArgumentOutOfRangeException(nameof(source), "Invalid TermSource.")
        };
    
        targetDictionary.TryGetValue(term, out var count);
        targetDictionary[term] = count + 1;

    }

}

