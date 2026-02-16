namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// Defines methods for standardizing search terms and phrases to ensure consistent search results.
/// </summary>
public interface IQueryNormalizer
{
	/// <summary>
	/// Normalizes a phrase by removing surrounding quotes and standardizing casing.
	/// </summary>
	/// <param name="quoted">The phrase string, potentially wrapped in quotes.</param>
	string NormalizePhrase(string quoted);

	/// <summary>
	/// Normalizes a single term by trimming whitespace and converting to lowercase.
	/// </summary>
	/// <param name="s">The raw term string.</param>
	string NormalizeTerm(string s);
}