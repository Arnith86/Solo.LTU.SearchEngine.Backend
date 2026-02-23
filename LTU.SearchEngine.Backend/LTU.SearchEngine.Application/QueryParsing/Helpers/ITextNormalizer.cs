namespace LTU.SearchEngine.Application.QueryParsing.Helpers
{
    /// <summary>
    /// Defines a contract for normalizing text tokens within the search engine.
    /// The interface is generic to support various token types while ensuring 
    /// consistent text processing across the application.
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
}
