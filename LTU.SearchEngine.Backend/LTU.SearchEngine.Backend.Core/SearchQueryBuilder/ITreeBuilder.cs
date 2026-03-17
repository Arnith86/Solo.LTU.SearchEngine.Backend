using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

/// <summary>
/// Defines a contract for transforming a sequence of tokens into an Abstract Syntax Tree (AST).
/// </summary>
/// <typeparam name="TResult">The return type produced by a visitor when traversing the generated tree.</typeparam>
/// <typeparam name="TType">The type of tokens used as input for tree construction.</typeparam>

public interface ITreeBuilder<TResult, TType>
{
	/// <summary>
	/// Parses a collection of tokens and constructs a hierarchical tree of <see cref="QueryNode{TResult}"/>.
	/// </summary>
	/// <param name="tokens">An enumerable collection of tokens to be transformed into a tree structure.</param>
	/// <returns>The root <see cref="QueryNode{TResult}"/> of the constructed tree.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="tokens"/> is null.</exception>
	/// <exception cref="System.InvalidOperationException">
	/// Thrown when the token sequence is malformed or results in an incomplete tree structure.
	/// </exception>
	QueryNode<TResult> BuildTree(IEnumerable<TType> tokens);
}