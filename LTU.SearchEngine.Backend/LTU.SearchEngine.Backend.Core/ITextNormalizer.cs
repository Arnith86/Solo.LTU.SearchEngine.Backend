namespace LTU.SearchEngine.Backend.Core;

/// <summary>
/// Defines a contract for normalizing objects into a string representation.
/// </summary>
/// <typeparam name="T">The type of token to be normalized.</typeparam>
public interface ITextNormalizer<T>
{
    /// <summary>
    /// Processes and normalizes the provided input.
    /// </summary>
    /// <param name="token">The object to normalize.</param>
    /// <returns>A normalized string representation of the input.</returns>
    string? Normalize(T token, string languageCode);
}
