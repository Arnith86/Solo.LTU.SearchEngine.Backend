namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

public class TermNode<T> : QueryNode<T>
{
	public string Term { get; }

	public TermNode(string term)
	{
		if (string.IsNullOrWhiteSpace(term))
			throw new ArgumentNullException(nameof(term), "must have a value.");

		Term = term;
	}

	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);
}
