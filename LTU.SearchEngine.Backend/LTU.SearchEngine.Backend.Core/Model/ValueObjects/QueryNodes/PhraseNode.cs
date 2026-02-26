namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;


/// <summary>
/// Represents a leaf node in the query tree containing a sequence of tokens that form an exact search phrase.
/// </summary>
/// <typeparam name="T">The return type produced by a visitor during tree traversal.</typeparam>
/// <remarks>
/// A PhraseNode is used to represent quoted strings from the user input, ensuring that the <br />
/// included tokens are treated as a single, ordered unit during the search process.
/// </remarks>
public class PhraseNode<T> : QueryNode<T>
{
	public List<ExtractedQueryToken> Phrase { get; }

	/// <summary>Initializes a new instance of the <see cref="PhraseNode{T}"/> class.</summary>
	/// <param name="phrase">A list of <see cref="ExtractedQueryToken"/> forming the phrase. Cannot be null.</param>
	/// <exception cref="ArgumentNullException">Thrown if the provided phrase list is null.</exception>
	public PhraseNode(List<ExtractedQueryToken> phrase)
	{
		Phrase = phrase ??
			throw new ArgumentNullException(nameof(phrase), "must have a value.");
	}

	/// <inheritdoc>/>
	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);
}