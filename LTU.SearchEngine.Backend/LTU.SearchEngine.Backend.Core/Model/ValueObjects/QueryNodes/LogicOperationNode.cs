using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;


/// <summary>
/// Represents a binary operator node in the query tree that connects two sub-expressions.
/// </summary>
/// <typeparam name="T">The return type produced by a visitor during tree traversal.</typeparam>
/// <remarks>
/// This node is a structural component of the AST, used to represent logical operations <br />
/// such as AND and OR by linking a left and a right <see cref="QueryNode{T}"/>.
/// </remarks>
public class LogicOperationNode<T> : QueryNode<T>
{
	public QueryNode<T> LeftNode { get; }
	public QueryNode<T> RightNode { get; }
	public LogicalOperators LogicalOperator { get; }

	/// <summary>Initializes a new instance of the <see cref="LogicOperationNode{T}"/> class.</summary>
	/// <param name="leftNode">The left sub-expression. Cannot be null.</param>
	/// <param name="rightNode">The right sub-expression. Cannot be null.</param>
	/// <param name="logicalOperator ">Enum representing the logical operator.param>
	/// <exception cref="ArgumentNullException">Thrown if any of the provided nodes are null.</exception>
	public LogicOperationNode(
		QueryNode<T> leftNode,
		QueryNode<T> rightNode,
		LogicalOperators logicalOperator
		)
	{
		LeftNode = leftNode ??
			throw new ArgumentNullException(nameof(leftNode));
		RightNode = rightNode ??
			throw new ArgumentNullException(nameof(rightNode));
		
		ValidateLogicalOperator(logicalOperator);

		LogicalOperator = logicalOperator;
	}

	/// <inheritdoc>/>
	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);

	private void ValidateLogicalOperator(LogicalOperators logicalOperator)
	{
		if (logicalOperator != LogicalOperators.AND &&
			logicalOperator != LogicalOperators.OR &&
			logicalOperator != LogicalOperators.NOT
		)
		{
			throw new ArgumentException(
				"Invalid logical operator. Only AND, OR, and NOT are allowed.",
				nameof(logicalOperator)
			);
		}
	}

}
