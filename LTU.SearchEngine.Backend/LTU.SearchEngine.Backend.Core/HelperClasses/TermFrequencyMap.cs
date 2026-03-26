namespace LTU.SearchEngine.Backend.Core.HelperClasses;

public class TermFrequencyMap
{
    private readonly Dictionary<string, int> _terms = new();

    public void AddTerm(string term)
    {
        if (term == null) throw new ArgumentNullException(nameof(term));

        if (string.IsNullOrWhiteSpace(term))
            throw new ArgumentException("Term cannot be empty or whitespace.", nameof(term));

        _terms.TryGetValue(term, out var count);
        _terms[term] = count + 1;
    }

    public IReadOnlyDictionary<string, int> ToReadOnly() => _terms;
}