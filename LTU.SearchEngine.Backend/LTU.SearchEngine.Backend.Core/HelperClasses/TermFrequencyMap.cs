namespace LTU.SearchEngine.Backend.Core.HelperClasses;

/// <summary>
/// Provides a mechanism to track and aggregate the frequency of specific terms within a document field.
/// </summary>
/// <remarks>
/// This helper is used during the indexing process to count occurrences of normalized terms 
/// before they are finalized into a read-only format for the index.
/// </remarks>
public class TermFrequencyMap : ITermMapper<IReadOnlyDictionary<string, int>>//<string, int>
{
    private readonly Dictionary<string, int> _terms = new();

    /// <summary>
    /// Increments the frequency count for the specified term. 
    /// If the term does not exist, it is added to the map with a count of 1.
    /// </summary>
    /// <param name="term">The normalized string term to be counted.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="term"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="term"/> is empty or contains only white space.</exception>
    public void AddTerm(string term)
    {
        if (term == null) throw new ArgumentNullException(nameof(term));

        if (string.IsNullOrWhiteSpace(term))
            throw new ArgumentException("Term cannot be empty or whitespace.", nameof(term));

        _terms.TryGetValue(term, out var count);
        _terms[term] = count + 1;
    }

    /// <summary>
    /// Converts the current state of the map into a read-only dictionary.
    /// </summary>
    /// <returns>A thread-safe, read-only view of the terms and their respective frequencies.</returns>
    public IReadOnlyDictionary<string, int> ToReadOnly() => _terms;
}