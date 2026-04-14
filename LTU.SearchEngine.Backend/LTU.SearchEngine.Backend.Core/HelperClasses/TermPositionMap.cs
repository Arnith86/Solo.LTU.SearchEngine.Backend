namespace LTU.SearchEngine.Backend.Core.HelperClasses;

/// <summary>
/// A specialized mapper that maintains the sequential order of terms as they appear in a document.
/// This is used to build positional indices, enabling advanced search features like phrase matching.
/// </summary>
/// <remarks>
/// Unlike a frequency map, this class preserves duplicates to ensure every occurrence 
/// of a term is recorded at its specific index.
/// </remarks>
public class TermPositionMap : ITermMapper<IReadOnlyList<string>>
{
    private readonly List<string> _terms = new List<string>();

    /// <summary>
    /// Appends a term to the end of the current sequence.
    /// </summary>
    /// <param name="term">The normalized term to be added.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="term"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="term"/> is empty or whitespace.</exception>
    public void AddTerm(string term)
    {   
        if (term == null) throw new ArgumentNullException(nameof(term));

        if (string.IsNullOrWhiteSpace(term))
            throw new ArgumentException("Term cannot be empty or whitespace.", nameof(term));

            _terms.Add(term);
    }

    
    /// <inheritdoc/>
    public IReadOnlyList<string> ToReadOnly() => _terms.AsReadOnly();
}