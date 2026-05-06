using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

/// <summary>
/// Implements the Shunting-yard algorithm to parse search queries. <br/>
/// It converts an infix notation query (e.g., "A AND B") into postfix notation (Reverse Polish Notation), <br/>
/// handling logical operator precedence (NOT > AND > OR) and grouping via parentheses.
/// </summary>
public class SearchQueryShuntingYardParser : IShuntingYardParser<ExtractedQueryToken>
{
	/// <inheritdoc/>
	public Queue<ExtractedQueryToken> ConvertToPostfix(IEnumerable<ExtractedQueryToken> tokens)
	{
		// ToDo: Figure out a solution to handle required tokens.
		VerifyTokens(tokens);

		// Queue<ExtractedQueryToken> outputQueue = new();
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
                // outputQueue.Enqueue(token);
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
                    // outputQueue.Enqueue(operatorStack.Pop());
                    postFixResults.Add(operatorStack.Pop());
                }

                HandleMismatchingClosingParentheses(operatorStack);

                // Discard the start parenthesis from the stack.
                var openingBracket = operatorStack.Pop();
                
                if (openingBracket.RequirementLevel.Equals(RequirementLevel.Required))
                    MarkLastOperatorAsRequired(postFixResults);
            
                // if (operatorStack.Count > 0) operatorStack.Pop();

            }
            else if (currTokenIsLogicalOperator)
            {
                int currentOperatorValue = GetPrecedenceValue(token.Token);

                while (NextPopDoesNotHavePrecedence(operatorStack, currentOperatorValue))
                    // outputQueue.Enqueue(operatorStack.Pop());
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

            // outputQueue.Enqueue(remainingToken);
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
