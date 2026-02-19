using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Text;
using System.Text.RegularExpressions;

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
		

		for (int index = 0; index < input.Length; index++)
		{
			int indexOut = 0;
			LoopAction action = LoopAction.None;
			var character = input[index];

			// AND, OR, are exceptions and are handled in the next method.
			(action, indexOut) = 
				TryHandleLogicalOperator(input, tokens, stringBuilder, index, character);
			
			if (action.Equals(LoopAction.Continue))
			{
				index += indexOut;
				continue;
			}

			(action, indexOut) = 
				TryHandleIsCapitalLetterOperator(input, tokens, stringBuilder, index);
			
			if (action.Equals(LoopAction.Continue))
			{ 
				index += indexOut;
				continue;
			}

			// Found start of a phrase
			if (IsEdgeOfPhrase(input, index, character))
			{
				isBuildingAPhrase = !isBuildingAPhrase;
				continue;
			}

			// Appends phrase characters
			action = TryHandleIsEdgeOfPhrase(
				input, tokens, stringBuilder, ref isBuildingAPhrase, index, character
			);
			
			if (action.Equals(LoopAction.Continue))	continue;
			
			// If this is reached must be term
			if (IsTokenTerm(character))
			{
				Flush(stringBuilder, tokens, QueryTokenType.Term);
				continue;
			}
			else
			{
				stringBuilder.Append(character);
			}
		}

		// Handles the last term if there is one
		Flush(stringBuilder, tokens, QueryTokenType.Term);

		return tokens;
	}




	private (LoopAction loopAction, int indexJump) TryHandleLogicalOperator(
		string input, 
		List<ExtractedQueryToken> tokens, 
		StringBuilder stringBuilder, 
		int index, 
		char character
		)
	{
		if (IsLogicalOperator(input, index, character))
		{
			if (IsDoubleLogicalOperator(input, index, character))
			{
				stringBuilder.Append(input, index++, 2);
				Flush(stringBuilder, tokens, QueryTokenType.LogicalOperator);
				return (LoopAction.Continue, 1);
			}

			stringBuilder.Append(character);
			Flush(stringBuilder, tokens, QueryTokenType.LogicalOperator);
			return (LoopAction.Continue, 0);
		}

		return (LoopAction.None, 0);
	}

	private (LoopAction loopAction, int indexOut ) TryHandleIsCapitalLetterOperator(
		string input,
		List<ExtractedQueryToken> tokens,
		StringBuilder stringBuilder,
		int index
		)
	{
		if (IsCapitalLetterOperator(input, index))
		{
			int length = input.AsSpan(index).StartsWith("AND") ? 3 : 2;

			stringBuilder.Append(input, index, length);
			Flush(stringBuilder, tokens, QueryTokenType.LogicalOperator);

			index += (length - 1);
			return (LoopAction.Continue, length - 1);
		}

		return (LoopAction.None, 0);
	}


	private LoopAction TryHandleIsEdgeOfPhrase(
		string input,
		List<ExtractedQueryToken> tokens,
		StringBuilder stringBuilder,
		ref bool isBuildingAPhrase,
		int index,
		char character
		)
	{
		if (isBuildingAPhrase)
		{
			// Found end of phrase, builds string
			if (IsEdgeOfPhrase(input, index, character, checkEndPhrase: true))
			{
				isBuildingAPhrase = !isBuildingAPhrase;
				Flush(stringBuilder, tokens, QueryTokenType.Phrase);
				return LoopAction.Continue;
			}

			stringBuilder.Append(character);
			return LoopAction.Continue;
		}

		return LoopAction.None;
	}


	private bool IsLogicalOperator(string input, int index, char character)
	{
		if ((index.Equals(0) || char.IsWhiteSpace(input[index - 1])) &&
			Regex.IsMatch(character.ToString(), @"[\+\-\!\&\|]")
			)
		{
			return true;			
		}

		return false;
	}

	private bool IsDoubleLogicalOperator(string input, int index, char character)
	{
		return 
			IsNotNullIndex(index + 1, input.Length) && 
			input[index + 1].Equals(character);
	}

	private bool IsCapitalLetterOperator(string input, int index)
	{
		if (IsNotNullIndex(index - 1, input.Length) && 
			char.IsWhiteSpace(input[index - 1]))
		{
			if (DoesWordMatch(input, index, "AND")) return true;
			if (DoesWordMatch(input, index, "OR")) return true;
		}

		return false;
	}

	private bool DoesWordMatch(string input, int index, string word)
	{
		// Enough indexes left?
		if (index + word.Length > input.Length) return false;

		for (int i = 0; i < word.Length; i++)
			if (input[index + i] != word[i]) return false;

		int nextCharIndex = index + word.Length;
		
		return true;
	}

	// Finalizes a token build
	/// <inheritdoc/>
	public void Flush(
		StringBuilder stringBuilder, 
		List<ExtractedQueryToken> tokens,
		QueryTokenType queryTokenType)
	{
		if (stringBuilder.Length == 0) return;

		var token = stringBuilder.ToString().Trim();
		var extractedToken = new ExtractedQueryToken(queryTokenType, token);
		
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
