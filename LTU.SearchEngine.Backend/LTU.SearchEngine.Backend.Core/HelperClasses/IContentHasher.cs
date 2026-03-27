namespace LTU.SearchEngine.Backend.Core.HelperClasses;

public interface IContentHasher
{
    /// <summary>
    /// Computes a SHA256 hash for the specified string content and returns it as a hexadecimal string.
    /// </summary>
    /// <param name="content">The string content to be hashed.</param>
    /// <returns>
    /// A 64-character hexadecimal string representing the SHA256 hash. <br/>
    /// Returns <see cref="string.Empty"/> if the input content is null or empty.
    /// </returns>
    string CalculateHash(byte[] content);
}