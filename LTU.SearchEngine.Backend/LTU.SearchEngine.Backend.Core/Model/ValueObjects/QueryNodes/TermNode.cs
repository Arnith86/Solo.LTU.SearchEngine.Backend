namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;


/// <summary>
/// Represents a leaf node in the query tree containing a single search term.
/// </summary>
/// <typeparam name="T">The return type produced by a visitor during tree traversal.</typeparam>
/// <remarks>
/// A TermNode is an atomic element in the Abstract Syntax Tree (AST), representing <br />
/// an unquoted keyword provided by the user.
/// </remarks>
public class TermNode<T> : QueryNode<T>
{
	public string Term { get; }

	/// <summary>Initializes a new instance of the <see cref="TermNode{T}"/> class.</summary>
	/// <param name="term">The search term string. Cannot be null or whitespace.</param>
	/// <exception cref="ArgumentNullException">Thrown if the provided term is null or empty.</exception>
	public TermNode(string term)
	{
		if (string.IsNullOrWhiteSpace(term))
			throw new ArgumentNullException(nameof(term), "must have a value.");

		Term = term;
	}


	/// <inheritdoc>/>
	public override T Accept(IQueryVisitor<T> visitor)
		=> visitor.Visit(this);
}
