namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

public class RequiredNode<T> : QueryNode<T>
{
	public QueryNode<T> Node { get; }

	public RequiredNode(QueryNode<T> node)
	{
		Node = node ?? 
			throw new ArgumentNullException(nameof(node));
	}

	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);
}