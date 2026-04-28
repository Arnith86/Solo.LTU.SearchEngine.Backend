using System.Runtime.CompilerServices;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;


/// <summary>
/// Represents a leaf node in the query tree containing a sequence of tokens that form an exact search phrase.
/// </summary>
/// <typeparam name="T">The return type produced by a visitor during tree traversal.</typeparam>
/// <remarks>
/// A PhraseNode is used to represent quoted strings from the user input, ensuring that the <br />
/// included tokens are treated as a single, ordered unit during the search process.
/// </remarks>
public class PhraseNode<T> : QueryNode<T>, IIsVoidable
{
	public List<ExtractedQueryToken> Phrase { get; }

	/// <summary>Initializes a new instance of the <see cref="PhraseNode{T}"/> class.</summary>
	/// <param name="phrase">A list of <see cref="ExtractedQueryToken"/> forming the phrase. Cannot be null.</param>
	/// <exception cref="ArgumentNullException">Thrown if the provided phrase list is null.</exception>
	/// <exception cref="ArgumentException">Thrown if the provided phrase list is empty.</exception>
	public PhraseNode(List<ExtractedQueryToken> phrase)
	{
		ValidatePhrase(phrase);
		Phrase = phrase;
	}

	/// <inheritdoc/>
	public override Task<T> AcceptAsync(IQueryVisitor<T> visitor)
		=> visitor.VisitAsync(this);

	public bool IsVoid() 
		=>  Phrase == null || Phrase.Count == 0 || Phrase.All( t => string.IsNullOrWhiteSpace(t.Token));

	/// <summary>
	/// Used for debugging and visualization purposes, returns the phrase as a <br/>
	/// single string contained in this node.
	/// </summary>
	/// <returns>The currently stored phrase as a single string.</returns>
	public override string ToString() 
		=> $"\"{string.Join(" ", Phrase.Select(t => t.Token))}\"";

	private void ValidatePhrase(List<ExtractedQueryToken> phrase)
	{
		if (phrase is null)
			throw new ArgumentNullException(nameof(phrase), "must have a value.");

		// if (phrase.Count < 1)
		// 	throw new ArgumentException(nameof(phrase), "cannot be empty.");
	}
}