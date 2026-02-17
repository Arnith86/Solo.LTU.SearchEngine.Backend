using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// Implementation of <see cref="ITokenizer"/> that handles whitespace separation 
/// and quote-aware grouping.
/// </summary>
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
		bool isBuildingAPhrase = false;

		for (int index=0; index < input.Length; index++)
		{
			var character = input[index];

			if (IsEdgeOfPhrase(input, index, character))
			{
				isBuildingAPhrase = !isBuildingAPhrase;
				continue;
			}

			if (isBuildingAPhrase)
			{
				if (IsEdgeOfPhrase(input, index, character, checkEndPhrase: true))
				{ 
					isBuildingAPhrase = !isBuildingAPhrase;
					continue;
				}

				stringBuilder.Append(character);
				continue;
			}

			if (char.IsWhiteSpace(character) && !isBuildingAPhrase)
			{
				Flush(stringBuilder, tokens);
				continue;
			}

			stringBuilder.Append(character);
		}

		Flush(stringBuilder, tokens);

		return tokens;
	}


	// Finalizes a token build
	/// <inheritdoc/>
	public void Flush(StringBuilder stringBuilder, List<string> tokens)
	{
		if (stringBuilder.Length == 0) return;

		var t = stringBuilder.ToString().Trim();
		if (t.Length > 0)
			tokens.Add(t);

		stringBuilder.Clear();
	}
	
	private bool IsEdgeOfPhrase(
		string input, 
		int index, 
		char character, 
		bool checkEndPhrase = false
		)
	{
		if (checkEndPhrase) 
			return input[index].Equals('"');
				
		return  IsNotNullIndex(index + 1, input.Length) &&
				input[index].Equals('"') &&
				ContainsEndQuote(input, index + 1);
	}
	

	private bool ContainsEndQuote(string input, int index)
	{
		for (int i = index; i < input.Length - 1; i++)
		{
			var character = input[i];
			if (input[i].Equals('"')) return true;
		}

		return false;
	}
	
	// ToDo: extract to own static helper class
	private bool IsNotNullIndex(int index, int length) 
		=> index >= 0 && index < length;
}
