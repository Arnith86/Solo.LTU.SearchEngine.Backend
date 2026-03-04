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
		// ToDo: Figure out a solution to handle implicit OR (word1 word2).
		VerifyTokens(tokens);

		Queue<ExtractedQueryToken> outputQueue = new();
		Stack<ExtractedQueryToken> operatorStack = new();

		foreach (ExtractedQueryToken token in tokens)
		{
			if (IsTermOrPhrase(token))
			{
				outputQueue.Enqueue(token);
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
					outputQueue.Enqueue(operatorStack.Pop());
				}

				// If stack is empty then there is a "(" missing.
				if (operatorStack.Count == 0 || !IsStartParentheses(operatorStack.Peek()))
				{
					throw new FormatException(
						"Mismatched parentheses: Found closing parenthesis ')' without a matching opening parenthesis."
					);
				}

				// Discard the start parenthesis from the stack.
				if (operatorStack.Count > 0) operatorStack.Pop();
			}
			else if (IsLogicalOperator(token))
			{
				int currentOperatorValue = GetPrecedenceValue(token.Token);

				while (NextPopDoesNotHavePrecedence(operatorStack, currentOperatorValue))
					outputQueue.Enqueue(operatorStack.Pop());

				operatorStack.Push(token);
			}
		}

		// Empty any remaining operators into the output queue
		while (operatorStack.Count > 0)
		{
			var remainingToken = operatorStack.Pop();

			if (IsStartParentheses(remainingToken))
				throw new FormatException(
					"Mismatched parentheses: Found opening parenthesis '(' without a matching closing parenthesis."
				);

			outputQueue.Enqueue(remainingToken);
		}

		return outputQueue;
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
