namespace LTU.SearchEngine.Backend.Core.TextNormalization;

/// <summary>
/// Filters out tokens that consist entirely of punctuation or symbols, 
/// ensuring only "searchable" terms reach the indexing and query pipelines.
/// </summary>
/// <remarks>
/// This filter acts as a lightweight validation step. It does not modify the text; 
/// rather, it decides whether a token has enough semantic substance (at least one letter or digit) 
/// to be useful. This allows technical terms like "C++" or "e-post" to pass through, 
/// while rejecting "noise" like "???" or "---".
/// </remarks>
public class NoiseFilter : INoiseFilter
{
    /// <summary>
    /// Evaluates whether the provided term contains any alphanumeric characters.
    /// </summary>
    /// <param name="rawTerm">The raw string token to evaluate.</param>
    /// <param name="languageCode">The ISO language code (e.g., "sv" or "en"). Currently unused as alphanumeric checks are Unicode-standard.</param>
    /// <returns>
    /// The original <paramref name="rawTerm"/> if it contains at least one letter or digit; 
    /// otherwise, <c>null</c> if the term is purely symbol-based or whitespace.
    /// </returns>
    public string? Apply(string rawTerm, string languageCode = "en")
    {
        if (string.IsNullOrWhiteSpace(rawTerm))
            return null;

        bool isSearchable = false;

        foreach (var character in rawTerm)
        {
            if (char.IsLetterOrDigit(character))
            {
                isSearchable = true;
                break;
            }
        }

        return isSearchable ? rawTerm : null;
    }
}
