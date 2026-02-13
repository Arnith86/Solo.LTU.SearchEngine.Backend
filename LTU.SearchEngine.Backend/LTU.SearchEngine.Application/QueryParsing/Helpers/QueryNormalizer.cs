namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

public class QueryNormalizer : IQueryNormalizer
{
	// For UC-3001 we normalize to lowercase to match common indexing/search behavior.
	public string NormalizeTerm(string s) =>
		s.Trim().ToLowerInvariant();

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
