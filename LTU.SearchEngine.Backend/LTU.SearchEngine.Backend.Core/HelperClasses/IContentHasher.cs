namespace LTU.SearchEngine.Backend.Core.HelperClasses;

/// <summary>
/// Provides utility methods for generating consistent cryptographic hashes of content.
/// </summary>
public interface IContentHasher
{
    // <summary>
    /// Computes a SHA256 hash for the specified byte array and returns it as a hexadecimal string.
    /// </summary>
    /// <param name="content">The raw byte content to be hashed.</param>
    /// <returns>
    /// A 64-character hexadecimal string representing the SHA256 hash. <br/>
    /// Returns <see cref="string.Empty"/> if the input is null or has a length of zero.
    /// </returns>
    string CalculateHash(byte[] content);

    /// <summary>
    /// Computes a SHA256 hash for the specified string content by encoding it as UTF-8 
    /// and returns it as a hexadecimal string.
    /// </summary>
    /// <remarks>
    /// This method ensures consistency with the <see cref="CalculateHash(byte[])"/> overload 
    /// by converting the string to bytes using UTF-8 encoding before hashing.
    /// </remarks>
    /// <param name="content">The string content to be hashed.</param>
    /// <returns>
    /// A 64-character hexadecimal string representing the SHA256 hash. <br/>
    /// Returns <see cref="string.Empty"/> if the input string is null or empty.
    /// </returns>
    string CalculateHash(string content);
}