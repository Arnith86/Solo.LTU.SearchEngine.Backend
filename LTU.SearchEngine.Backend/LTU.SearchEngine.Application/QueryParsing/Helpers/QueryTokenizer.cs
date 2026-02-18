using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
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
	/// Example: cat "hello dolly" dog -> [cat, hello dolly, dog]
	/// </summary>
	public List<ExtractedQueryToken> Tokenize(string input)
	{
		var tokens = new List<ExtractedQueryToken>();
		var stringBuilder = new StringBuilder();
		bool isBuildingAPhrase = false;

		for (int index=0; index < input.Length; index++)
		{
			var character = input[index];

			// Found start of a phrase
			if (IsEdgeOfPhrase(input, index, character))
			{
				isBuildingAPhrase = !isBuildingAPhrase;
				continue;
			}

			// Appends phrase characters
			if (isBuildingAPhrase)
			{
				// Found end of phrase, builds string
				if (IsEdgeOfPhrase(input, index, character, checkEndPhrase: true))
				{
					isBuildingAPhrase = !isBuildingAPhrase;
					Flush(stringBuilder, isPhrase: true, tokens);
					continue;
				}

				stringBuilder.Append(character);
				continue;
			}

			// If this is reached must be term
			if (IsTokenTerm(character))
			{
				Flush(stringBuilder, isPhrase: false, tokens); 
				continue;
			}
			else
			{
				stringBuilder.Append(character);
			}
		}

		// Handles the last term if there is one
		Flush(stringBuilder, isPhrase: false, tokens);

		return tokens;
	}


	// Finalizes a token build
	/// <inheritdoc/>
	public void Flush(
		StringBuilder stringBuilder, 
		bool isPhrase, 
		List<ExtractedQueryToken> tokens
		)
	{
		if (stringBuilder.Length == 0) return;

		QueryTokenType tokenType = 
			isPhrase ? QueryTokenType.Phrase : QueryTokenType.Term;
		
		var token = stringBuilder.ToString().Trim();
		var extractedToken = new ExtractedQueryToken(tokenType, token);
		
		if (token.Length > 0) 
			tokens.Add(extractedToken);
		
		stringBuilder.Clear();
	}


	private bool IsEdgeOfPhrase(
		string input,
		int index,
		char character,
		bool checkEndPhrase = false
		)
	{
		if (checkEndPhrase) return input[index].Equals('"');

		return input[index].Equals('"') &&
				IsNotNullIndex(index + 1, input.Length) &&
				ContainsEndQuote(input, index + 1);
	}


	private bool ContainsEndQuote(string input, int index)
	{
		for (int i = index; i < input.Length - 1; i++)
			if (input[i].Equals('"')) return true;

		return false;
	}

	private bool IsTokenTerm(char character) => char.IsWhiteSpace(character);

	// ToDo: extract to own static helper class
	private bool IsNotNullIndex(int index, int length)
		=> index >= 0 && index < length;
}
