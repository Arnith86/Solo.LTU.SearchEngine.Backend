namespace LTU.SearchEngine.Backend.Core;

/// <summary>
/// Defines a contract for normalizing objects into a string representation.
/// </summary>
/// <typeparam name="T">The type of token to be normalized.</typeparam>
public interface ITextNormalizer<T>
    {
        /// <summary>
        /// Processes and normalizes the content of a given token, such as 
        /// converting text to lowercase or handling specific character formatting.
        /// </summary>
        /// <param name="token">The token object containing the text to normalize.</param>
        /// <returns>A normalized string representation of the provided token.</returns>
        string Normalize(T token);
    }
