namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

public class PhraseNode<T> : QueryNode<T>
{
	public List<ExtractedQueryToken> Phrase { get; }

	public PhraseNode(List<ExtractedQueryToken> phrase)
	{
		Phrase = phrase ??
			throw new ArgumentNullException(nameof(phrase), "must have a value.");
	}

	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);
}