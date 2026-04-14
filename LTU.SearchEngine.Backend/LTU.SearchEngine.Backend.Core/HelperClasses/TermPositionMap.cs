namespace LTU.SearchEngine.Backend.Core.HelperClasses;


public class TermPositionMap : ITermMapper<IReadOnlyList<string>>
{
    private readonly List<string> _terms = new List<string>();

    public void AddTerm(string term)
    {
        if (term == null) throw new ArgumentNullException(nameof(term));

        if (string.IsNullOrWhiteSpace(term))
            throw new ArgumentException("Term cannot be empty or whitespace.", nameof(term));

            _terms.Add(term);
    }

    public IReadOnlyList<string> ToReadOnly() => _terms.AsReadOnly();
}