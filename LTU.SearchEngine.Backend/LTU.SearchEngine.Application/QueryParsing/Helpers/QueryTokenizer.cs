using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

public class QueryTokenizer : ITokenizer
{
	/// <summary>
	/// Tokenizes input by whitespace while keeping quoted phrases together (including quotes).
	/// Example: cat "hello dolly" dog -> [cat, "hello dolly", dog]
	/// </summary>
	public List<string> Tokenize(string input)
	{
		var tokens = new List<string>();
		var stringBuilder = new StringBuilder();
		bool inQuotes = false;

		foreach (var c in input)
		{
			if (c == '"')
			{
				inQuotes = !inQuotes;
				stringBuilder.Append(c);
				continue;
			}

			if (char.IsWhiteSpace(c) && !inQuotes)
			{
				Flush(stringBuilder, tokens);
				continue;
			}

			stringBuilder.Append(c);
		}

		Flush(stringBuilder, tokens);

		return tokens;
	}

	// Finalizes a token build
	public void Flush(StringBuilder stringBuilder, List<string> tokens)
	{
		if (stringBuilder.Length == 0) return;

		var t = stringBuilder.ToString().Trim();
		if (t.Length > 0)
			tokens.Add(t);

		stringBuilder.Clear();
	}
}
