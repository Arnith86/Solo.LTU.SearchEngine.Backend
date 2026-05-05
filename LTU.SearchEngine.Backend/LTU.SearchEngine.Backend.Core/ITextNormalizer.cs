namespace LTU.SearchEngine.Backend.Core;

/// <summary>
/// Defines a contract for normalizing input data into a searchable format.
/// </summary>
/// <typeparam name="TToken">The type of the raw input (usually string).</typeparam>
/// <typeparam name="TReturn">The type of the result (e.g., string or IEnumerable of strings).</typeparam>
public interface ITextNormalizer<TToken, TReturn>
{   
    /// <summary>
    /// Processes and normalizes the provided input based on language-specific rules.
    /// </summary>
    /// <param name="token">The raw token or object to normalize.</param>
    /// <param name="languageCode">The language context used for stemming and stop-words.</param>
    /// <returns>The normalized representation of the input.</returns>
    TReturn Normalize(TToken token, string languageCode);
}
