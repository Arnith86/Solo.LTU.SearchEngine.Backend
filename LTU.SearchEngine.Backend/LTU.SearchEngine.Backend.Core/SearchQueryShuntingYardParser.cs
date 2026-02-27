using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Backend.Core;

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
				// Move operators to output until the matching start parenthesis is found
				while (ShouldPopOperator(operatorStack))
				{
					outputQueue.Enqueue(operatorStack.Pop());
				}

				// Discard the start parenthesis from the stack
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
			outputQueue.Enqueue(operatorStack.Pop());

		return outputQueue;
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
			"NOT" => 3,
			"AND" => 2,
			"OR" => 1,
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
