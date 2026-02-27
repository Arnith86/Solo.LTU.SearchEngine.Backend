using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Backend.Core;


public class SearchQueryShuntingYardAlgorithm
{
	public Queue<ExtractedQueryToken> ConvertToPostfix(List<ExtractedQueryToken> tokens)
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
				// Pops tokens until start of parenthesis is found
				while(
					operatorStack.TryPeek(out ExtractedQueryToken? result) && 
					!IsStartParentheses(result))
				{
					outputQueue.Enqueue(operatorStack.Pop());
				}
			}
			else if (IsLogicalOperator(token))
			{
				int currentOperatorValue = GetPrecedenceValue(token.Token);

				while (NextPopDoesNotHavePrecedence(operatorStack, currentOperatorValue))
					outputQueue.Enqueue(operatorStack.Pop());

				operatorStack.Push(token);
			}
		}

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
			"OR" => 1
		};
	}

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
