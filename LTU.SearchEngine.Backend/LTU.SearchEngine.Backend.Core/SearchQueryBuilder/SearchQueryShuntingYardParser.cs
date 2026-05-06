using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

/// <summary>
/// Implements an enhanced Shunting-yard algorithm to parse search queries into Postfix notation (RPN). <br/>
/// Beyond standard operator precedence (NOT > AND > OR), this implementation handles:
/// <list type="bullet">
///     <item><b>Requirement Persistence:</b> Individual terms maintain their <see cref="RequirementLevel"/> (e.g., +term).</item>
///     <item><b>Requirement Inheritance:</b> Propagates requirements from grouping operators to their root logical operators (e.g., +(A OR B) marks OR as Required).</item>
///     <item><b>Validation:</b> Prevents invalid semantic combinations like consecutive operators or applying NOT to a required term.</item>
/// </list>
/// </summary>
public class SearchQueryShuntingYardParser : IShuntingYardParser<ExtractedQueryToken>
{
	/// <inheritdoc/>
    /// <summary>
    /// Converts an infix sequence of tokens into a postfix queue while preserving and propagating requirement metadata.
    /// </summary>
    /// <param name="tokens">The stream of tokens extracted from the raw search string.</param>
    /// <returns>A queue of tokens in Reverse Polish Notation, ready for AST construction.</returns>
    /// <exception cref="FormatException">Thrown when encountering mismatched parentheses, consecutive operators, or illegal requirement applications (e.g., NOT +term).</exception>
    /// <exception cref="ArgumentNullException">Thrown if the token collection is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the token collection is empty.</exception> 
	public Queue<ExtractedQueryToken> ConvertToPostfix(IEnumerable<ExtractedQueryToken> tokens)
	{
		VerifyTokens(tokens);

		List<ExtractedQueryToken> postFixResults = new();
		Stack<ExtractedQueryToken> operatorStack = new();

		ExtractedQueryToken? lastNonGroupingToken = null;
		bool firstTermPhraseSet = false;
		
        var tokenList = tokens.ToList();

		for (int i = 0; i < tokenList.Count; i++)
        {
            var token = tokenList[i];

            bool currTokenIsLogicalOperator = IsLogicalOperator(token);

            HandleLogicalOperatorBeforeFirstTermOrPhrase(firstTermPhraseSet, token, currTokenIsLogicalOperator);
            HandleLogicalOperatorsInSequence(lastNonGroupingToken, token);

            if (CanVerifyRequirementLookahead(tokenList, i, currTokenIsLogicalOperator))
                HandleRequiredRequirement(token, tokenList[i + 1]);

            if (IsTermOrPhrase(token))
            {
                postFixResults.Add(token);
                firstTermPhraseSet = true;
            }
            else if (IsStartParentheses(token))
            {
                operatorStack.Push(token);
            }
            else if (IsEndParentheses(token))
            {
                // Move operators to output until the matching start parenthesis is found.
                while (ShouldPopOperator(operatorStack))
                {
                    postFixResults.Add(operatorStack.Pop());
                }

                HandleMismatchingClosingParentheses(operatorStack);

                // Discard the start parenthesis from the stack.
                var openingBracket = operatorStack.Pop();
                
                if (openingBracket.RequirementLevel.Equals(RequirementLevel.Required))
                    MarkLastOperatorAsRequired(postFixResults);
            }
            else if (currTokenIsLogicalOperator)
            {
                int currentOperatorValue = GetPrecedenceValue(token.Token);

                while (NextPopDoesNotHavePrecedence(operatorStack, currentOperatorValue))
                    postFixResults.Add(operatorStack.Pop());

                operatorStack.Push(token);
            }

            if (!IsGroupingOperator(token)) lastNonGroupingToken = token;
        }

        // Empty any remaining operators into the output queue
        while (operatorStack.Count > 0)
        {
            var remainingToken = operatorStack.Pop();

            HandleMismatchingStartParentheses(remainingToken);

            postFixResults.Add(remainingToken);
        }

        return new Queue<ExtractedQueryToken>(postFixResults);
	}

    private void MarkLastOperatorAsRequired(List<ExtractedQueryToken> outputQueue)
    {
        if (outputQueue.Count > 0)
        {
            var lastToken = outputQueue[^1];
            var requiredOperatorTokenVersion = new ExtractedQueryToken(
                tokenType: lastToken.TokenType,
                token: lastToken.Token,
                language: lastToken.Language,
                requirementLevel: RequirementLevel.Required
            ); 

            outputQueue[^1] = requiredOperatorTokenVersion;
        }

    }

    private static bool CanVerifyRequirementLookahead(
        List<ExtractedQueryToken> tokenList, 
        int index, 
        bool isLogicalOperator)
    {
        return isLogicalOperator && (index + 1 < tokenList.Count);
    }
    

    private void HandleRequiredRequirement(ExtractedQueryToken operatorToken, ExtractedQueryToken nextToken)
    {
        if (IsANotOperator(operatorToken) && nextToken.RequirementLevel.Equals(RequirementLevel.Required))
        {
            throw new FormatException(
                $"Invalid query format: The NOT operator cannot be applied to a required term or group. " +
                $"Found '{operatorToken.Token}' followed by a '+' requirement on '{nextToken.Token}'."
            );
        }
    }

    private void HandleMismatchingStartParentheses(ExtractedQueryToken remainingToken)
    {
        if (IsStartParentheses(remainingToken))
            throw new FormatException(
                "Mismatched parentheses: Found opening parenthesis '(' without a matching closing parenthesis."
            );
    }

    private void HandleMismatchingClosingParentheses(Stack<ExtractedQueryToken> operatorStack)
    {
		// If stack is empty then there is a "(" missing.
        if (operatorStack.Count == 0 || !IsStartParentheses(operatorStack.Peek()))
        {
            throw new FormatException(
                "Mismatched parentheses: Found closing parenthesis ')' without a matching opening parenthesis."
            );
        }
    }

    private void HandleLogicalOperatorsInSequence(
		ExtractedQueryToken? lastNonGroupingToken, 
		ExtractedQueryToken token)
    {
        if (lastNonGroupingToken is not null && 
			IsLogicalOperator(lastNonGroupingToken) && 
			IsLogicalOperator(token))
        {
            throw new FormatException(
                "Invalid query format: Consecutive logical operators are not allowed. " +
                $"Found '{lastNonGroupingToken.Token}' and '{token.Token}' in sequence."
            );
        }
    }

    private static void HandleLogicalOperatorBeforeFirstTermOrPhrase(
		bool firstTermPhraseSet, 
		ExtractedQueryToken token, 
		bool currTokenIsLogicalOperator)
    {
        if (currTokenIsLogicalOperator && !firstTermPhraseSet)
        {
            throw new FormatException(
                "Invalid query format: Query cannot start with a logical operator. " +
                $"Found '{token.Token}' at the beginning of the query."
            );
        }
    }

    private bool IsGroupingOperator(ExtractedQueryToken token) 
		=> token.TokenType.Equals(QueryTokenType.GroupingOperator);
    

    private bool IsANotOperator(ExtractedQueryToken token)
	{
		return token.Token switch
		{
			"NOT" or "!" or "-" => true,
			_ => false	
		};
	}

    private void VerifyTokens(IEnumerable<ExtractedQueryToken> tokens)
	{
		if (tokens is null)
			throw new ArgumentNullException(nameof(tokens), "must have a value.");
		if (!tokens.Any())
			throw new ArgumentOutOfRangeException(nameof(tokens), "cannot be empty.");
	}

	private bool NextPopDoesNotHavePrecedence(
		Stack<ExtractedQueryToken> operatorStack, 
		int currentOperatorValue
	)
	{
		return operatorStack.TryPeek(out ExtractedQueryToken? result) &&
							GetPrecedenceValue(result.Token) >= currentOperatorValue;
	}


	private int GetPrecedenceValue(string op)
	{
		return op switch
		{
			"NOT" or "!" or "-" => 3,
			"AND" or "&&" => 2,
			"OR" or "||" => 1,
			_ => 0
		};
	}
	
	private bool ShouldPopOperator(Stack<ExtractedQueryToken> operatorStack)
		=> operatorStack.TryPeek(out ExtractedQueryToken? result) &&
							!IsStartParentheses(result);
	

	private bool IsLogicalOperator(ExtractedQueryToken token)
		=> token.TokenType.Equals(QueryTokenType.LogicalOperator); 


	private bool IsEndParentheses(ExtractedQueryToken token)
		=>	token.TokenType.Equals(QueryTokenType.GroupingOperator) &&
			token.Token.Equals(")");


	private bool IsStartParentheses(ExtractedQueryToken token)
		=>	token.TokenType.Equals(QueryTokenType.GroupingOperator) && 
			token.Token.Equals("(");
	

	private bool IsTermOrPhrase(ExtractedQueryToken token)
		=>	token.TokenType.Equals(QueryTokenType.Term) || 
			token.TokenType.Equals(QueryTokenType.Phrase);
}
