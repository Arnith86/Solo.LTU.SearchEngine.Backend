namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

public class BinaryNode<T> : QueryNode<T>
{
	public QueryNode<T> LeftNode { get; }
	public QueryNode<T> RightNode { get; }
	public BinaryNode<T> OperatorNode { get; }
	
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

	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);
}
