using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Text;
using System.Text.RegularExpressions;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// Implementation of <see cref="IStringTokenizer"/> that handles operator recognition, whitespace <br/>
/// separation of terms and quote-aware grouping.
/// </summary>
public class QueryStringTokenizer : IStringTokenizer<ExtractedQueryToken, QueryTokenType>
{
	private readonly IQuerySyntaxHelper _syntaxHelper;

	public QueryStringTokenizer(IQuerySyntaxHelper syntaxHelper)
	{
		_syntaxHelper = syntaxHelper ?? 
			throw new ArgumentNullException(nameof(syntaxHelper));
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


	/// <inheritdoc/>
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

            // Checks implicit OR
            action = TryHandleImplicitOr(input, tokens, stringBuilder, ref isBuildingAPhrase, index);

            if (action.Equals(LoopAction.Continue)) continue;
            // Checks for grouping operators. ( ) [ ] { } 
            action = TryHandleIsGroupingOperator(tokens, stringBuilder, character);

			if (action.Equals(LoopAction.Continue))	continue;

			// AND, OR, NOT, are exceptions and are handled in the next method.
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

			if (action.Equals(LoopAction.Continue)) continue;

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

		_syntaxHelper.ValidateGrouping(tokens);

		return tokens;
	}

    private LoopAction TryHandleImplicitOr(
       string input,
       List<ExtractedQueryToken> tokens,
       StringBuilder stringBuilder,
       ref bool isBuildingAPhrase,
       int index)
    {
        char character = input[index];

        // Only handle whitespace outside phrases
        if (!char.IsWhiteSpace(character) || isBuildingAPhrase)
            return LoopAction.None;

        // No term being built → nothing to separate
        if (stringBuilder.Length == 0)
            return LoopAction.None;

        // Prevent implicit OR when term starts with quote (unclosed phrase case)
        if (stringBuilder[0] == '"')
            return LoopAction.None;

        // Look ahead to next character
        if (index + 1 < input.Length)
        {
            char next = input[index + 1];

            // Do not insert OR before phrases or operators
            if (next == '"' ||
                "!+-&|".Contains(next) ||
                IsCapitalLetterOperator(input, index + 1))
            {
                return LoopAction.None;
            }
        }

        int tokenCountBeforeFlush = tokens.Count;

        Flush(stringBuilder, tokens, QueryTokenType.Term);

        // Flush may not add a token if normalization removes it
        if (tokens.Count == tokenCountBeforeFlush)
            return LoopAction.Continue;

        tokens.Add(new ExtractedQueryToken(QueryTokenType.LogicalOperator,"OR"));

        return LoopAction.Continue;
    }

    private LoopAction TryHandleIsGroupingOperator(
		List<ExtractedQueryToken> tokens, StringBuilder stringBuilder, char character)
	{
		if (IsGroupingOperator(character))
		{
			stringBuilder.Append(character);
			Flush(stringBuilder, tokens, QueryTokenType.GroupingOperator);
			return LoopAction.Continue;
		}

		return LoopAction.None;
	}


	private bool IsGroupingOperator(char character) => "(){}[]".Contains(character);


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
			var span = input.AsSpan(index);
			int length = (span.StartsWith("AND") || span.StartsWith("NOT")) ? 3 : 2;

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
		bool isAtStartOrAfterSpace = FirstIndexOrAfterSpace(input, index);

		if (!isAtStartOrAfterSpace) return false;

		var remaining = input.AsSpan(index);

		
		if (remaining.StartsWith("NOT") && IsFullWord(remaining, 3)) return true;
		if (remaining.StartsWith("AND") && IsFullWord(remaining, 3)) return true;
		if (remaining.StartsWith("OR") && IsFullWord(remaining, 2)) return true;

		return false;
	}

	private bool FirstIndexOrAfterSpace(string input, int index)
	{
		// Start of string or directly after a space
		return index == 0 ||
			(IsNotNullIndex(index - 1, input.Length) && char.IsWhiteSpace(input[index - 1])
		);
	}

	// Makes sure that the while word is as only as long as the operator
	private bool IsFullWord(ReadOnlySpan<char> span, int length) =>	
		span.Length == length || char.IsWhiteSpace(span[length]);


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
