
using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <inheritdoc />
public class QuerySyntaxHelper : IQuerySyntaxHelper
{
	public void ValidateGrouping(List<ExtractedQueryToken> tokens)
	{
		var stack = new Stack<char>();
		
		var pairs = new Dictionary<char, char> {
			{ ')', '(' },
			{ ']', '[' },
			{ '}', '{' }
		};

		foreach (var token in tokens)
		{
			// Skips non operators or long operators
			if (token.Token.Length != 1) continue; 

			char character = token.Token[0];

			// Catches start grouping operators
			if (character == '(' || character == '[' || character == '{')
				stack.Push(character);
			
			// Check if correct closer is on top of stack.
			else if (pairs.ContainsKey(character))
			{
				if (stack.Count == 0 || stack.Pop() != pairs[character])
				{
					throw new InvalidQueryStringException(
						$"Mismatched or extra closing operator: {character}", token.Token);
				}
			}
		}

		if (stack.Count > 0)
		{
			throw new InvalidQueryStringException(
				$"Unclosed operator remaining: {stack.Peek()}", "");
		}
	}
}
