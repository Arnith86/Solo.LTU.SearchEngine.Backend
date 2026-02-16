namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// Implementation of <see cref="IQueryNormalizer"/> that provides basic string cleaning <br />
/// using invariant culture settings.
/// </summary>
public class QueryNormalizer : IQueryNormalizer
{
	/// <inheritdoc/>
	public string NormalizeTerm(string s) =>
		s.Trim().ToLowerInvariant();

	/// <inheritdoc/>
	public string NormalizePhrase(string quoted)
	{
		var inner = quoted.Trim();

		// Remove surrounding quotes
		if (inner.StartsWith("\"", StringComparison.Ordinal) &&
			inner.EndsWith("\"", StringComparison.Ordinal) &&
			inner.Length >= 2)
		{
			// Take everything from index 1 to last index.
			inner = inner[1..^1];
		}

		return inner.Trim().ToLowerInvariant();
	}
}
