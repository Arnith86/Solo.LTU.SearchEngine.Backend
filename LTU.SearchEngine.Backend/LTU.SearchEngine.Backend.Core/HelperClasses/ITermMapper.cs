namespace LTU.SearchEngine.Backend.Core.HelperClasses;

/// <summary>
/// Defines a contract for components that map and transform terms into a specific collection during the indexing process.
/// </summary>
/// <typeparam name="TCollection">
/// The type of read-only collection to be returned <br/>
/// (e.g., <see cref="IReadOnlyList{T}"/> or <see cref="IReadOnlyDictionary{TKey, TValue}"/>).
/// </typeparam>
public interface ITermMapper<TCollection>
{
    /// <summary>
    /// Adds a term to the internal collection and performs specific mapping logic 
    /// (e.g., incrementing frequency or storing the positional index).
    /// </summary>
    /// <param name="term">The string representation of the term to be mapped.</param>
    /// <exception cref="ArgumentNullException">Thrown when the term is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the term is empty or consists only of white-space characters.</exception>
    void AddTerm(string term);

    /// <summary>
    /// Converts the internal state into a read-only representation of the collected data.
    /// </summary>
    /// <returns>
    /// A collection of type <typeparamref name="TCollection"/> containing the mapped terms.
    /// </returns>
    TCollection ToReadOnly();
}