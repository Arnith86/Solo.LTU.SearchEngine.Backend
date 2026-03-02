using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

/// <summary>
/// Represents a unary operator node in the query tree that marks a sub-expression as required.
/// </summary>
/// <typeparam name="T">The return type produced by a visitor during tree traversal.</typeparam>
/// <remarks>
/// This node is typically used to represent the mandatory inclusion operator (e.g., '+') <br />
/// and wraps another <see cref="QueryNode{T}"/> to indicate it must be present in the search results.
/// </remarks>
public class RequiredNode<T> : QueryNode<T>
{
	public QueryNode<T> Node { get; }

	/// <summary>Initializes a new instance of the <see cref="RequiredNode{T}"/> class.</summary>
	/// <param name="node">The sub-expression to be marked as required. Cannot be null.</param>
	/// <exception cref="ArgumentNullException">Thrown if the provided node is null.</exception>
	public RequiredNode(QueryNode<T> node)
	{
		Node = node ?? 
			throw new ArgumentNullException(nameof(node));
	}

	/// <inheritdoc>/>
	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);
}