namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;


/// <summary>
/// Represents a binary operator node in the query tree that connects two sub-expressions.
/// </summary>
/// <typeparam name="T">The return type produced by a visitor during tree traversal.</typeparam>
/// <remarks>
/// This node is a structural component of the AST, used to represent logical operations <br />
/// such as AND and OR by linking a left and a right <see cref="QueryNode{T}"/>.
/// </remarks>
public class BinaryNode<T> : QueryNode<T>
{
	public QueryNode<T> LeftNode { get; }
	public QueryNode<T> RightNode { get; }
	public BinaryNode<T> OperatorNode { get; }

	/// <summary>Initializes a new instance of the <see cref="BinaryNode{T}"/> class.</summary>
	/// <param name="leftNode">The left sub-expression. Cannot be null.</param>
	/// <param name="rightNode">The right sub-expression. Cannot be null.</param>
	/// <param name="operatorNode">The node representing the logical operator. Cannot be null.</param>
	/// <exception cref="ArgumentNullException">Thrown if any of the provided nodes are null.</exception>
	public BinaryNode(
		QueryNode<T> leftNode,
		QueryNode<T> rightNode,
		BinaryNode<T> operatorNode
		)
	{
		LeftNode = leftNode ?? 
			throw new ArgumentNullException(nameof(leftNode));
		RightNode = rightNode ?? 
			throw new ArgumentNullException(nameof(rightNode));
		OperatorNode = operatorNode ?? 
			throw new ArgumentNullException(nameof(operatorNode));
	}

	/// <inheritdoc>/>
	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);
}
