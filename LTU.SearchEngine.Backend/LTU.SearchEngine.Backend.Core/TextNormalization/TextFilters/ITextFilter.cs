namespace LTU.SearchEngine.Backend.Core.TextNormalization;

/// <summary>
/// Defines a contract for a specific transformation or filtering step within 
/// the text normalization pipeline.
/// </summary>
/// <typeparam name="T">
/// The return type of the filtered result (e.g., <see cref="string"/> for 1:1 transformations 
/// or <see cref="IEnumerable{String}"/> for 1:N token explosions).
/// </typeparam>
public interface ITextFilter<T>
{
    /// <summary>
    /// Applies a specific transformation or filtering logic to the provided raw term.
    /// </summary>
    /// <param name="rawTerm">The string input to be processed.</param>
    /// <param name="languageCode">
    /// The ISO language code (defaulting to "sv") used to apply language-specific 
    /// filtering rules, such as stop-word lists or stemming algorithms.
    /// </param>
    /// <returns>
    /// The processed result of type <typeparamref name="T"/>. 
    /// Returns the default value of <typeparamref name="T"/> (typically null or an empty collection) 
    /// if the input is rejected by the filter.
    /// </returns>
    T Apply(string rawTerm, string languageCode = "sv");
}
