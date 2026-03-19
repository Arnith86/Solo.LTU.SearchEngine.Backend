namespace LTU.SearchEngine.Backend.Core;

/// <summary>
/// Defines the logic for validating whether a URL belongs to an authorized domain (white-list).
/// Used to prevent the crawler from leaving the intended target websites.
/// </summary>
public interface IDomainValidator
{
	/// <summary>Checks if the specified URL is authorized for crawling based on its domain.</summary>
    /// <param name="url">The full URL to validate (e.g., "https://www.ltu.se").</param>
    /// <returns>
    /// <c>true</c> if the URL's domain is in the white-list or is a sub-domain of an authorized domain; 
    /// otherwise, <c>false</c>.
    /// </returns>
	public bool IsWhitelisted(string url);  
}
