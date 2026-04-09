namespace LTU.SearchEngine.Backend.Core.HelperClasses;

using System.Security.Cryptography;
using System.Text;


/// <summary>
/// Provides utility methods for generating cryptographic hashes of content.
/// </summary>
public class ContentHasher : IContentHasher
{
    /// <inheritdoc />
    public string CalculateHash(byte[] content)
    {
        if (content == null || content.Length == 0)
            return string.Empty;

        var hashBytes = SHA256.HashData(content);

        return Convert.ToHexString(hashBytes);
    }

    /// <inheritdoc />
    public string CalculateHash(string content)
    {
        if (content == null || content.Length == 0)
            return string.Empty;

        var hashBytes = Encoding.UTF8.GetBytes(content);
        return CalculateHash(hashBytes);
    }
}
