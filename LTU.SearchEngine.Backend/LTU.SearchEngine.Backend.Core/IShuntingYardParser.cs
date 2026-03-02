namespace LTU.SearchEngine.Backend.Core;

/// <summary>
/// Defines a generic contract for transforming a collection of tokens from Infix notation <br />
/// to Postfix notation (Reverse Polish Notation) using the Shunting-yard algorithm.
/// </summary>
/// <typeparam name="T">The specific type of token to be processed (e.g., ExtractedQueryToken).</typeparam>
public interface IShuntingYardParser<T>
{
	/// <summary>
	/// Converts a sequence of tokens in Infix notation to a queue in Postfix notation.
	/// </summary>
	/// <remarks>
	/// Postfix notation removes the need for parentheses and explicitly defines <br />
	/// the order of operations based on operator precedence and position.
	/// </remarks>
	/// <param name="tokens">An enumerable collection of tokens in their original order (Infix).</param>
	/// <returns>A <see cref="Queue{T}"/> containing the tokens sorted according to Postfix logic.</returns>
	Queue<T> ConvertToPostfix(IEnumerable<T> tokens);
}
