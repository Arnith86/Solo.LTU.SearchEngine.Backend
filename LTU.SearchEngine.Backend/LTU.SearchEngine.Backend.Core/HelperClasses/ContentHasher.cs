namespace LTU.SearchEngine.Backend.Core.HelperClasses;

using System.Security.Cryptography;


/// <summary>
/// Provides utility methods for generating cryptographic hashes of content.
/// </summary>
public class ContentHasher : IContentHasher
{
    /// <summary>
    /// Computes a SHA256 hash for the specified string content and returns it as a hexadecimal string.
    /// </summary>
    /// <param name="content">The string content to be hashed.</param>
    /// <returns>
    /// A 64-character hexadecimal string representing the SHA256 hash. <br/>
    /// Returns <see cref="string.Empty"/> if the input content is null or empty.
    /// </returns>
    public string CalculateHash(byte[] content)
    {
        if (content == null || content.Length == 0)
            return string.Empty;

        var hashBytes = SHA256.HashData(content);

        return Convert.ToHexString(hashBytes);
    }
}
