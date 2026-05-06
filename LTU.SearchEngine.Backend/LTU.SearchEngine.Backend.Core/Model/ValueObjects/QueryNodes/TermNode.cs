using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;


/// <summary>
/// Represents a leaf node in the query tree containing a single search term.
/// </summary>
/// <typeparam name="T">The return type produced by a visitor during tree traversal.</typeparam>
/// <remarks>
/// A TermNode is an atomic element in the Abstract Syntax Tree (AST), representing <br />
/// an unquoted keyword provided by the user.
/// </remarks>
public class TermNode<T> : QueryNode<T>, IIsVoidable, IIsRequirable
{
	private bool _isRequired; 
	public string Term { get; }

	/// <summary>Initializes a new instance of the <see cref="TermNode{T}"/> class.</summary>
	/// <param name="term">The search term string. Cannot be null.</param>
	/// <exception cref="ArgumentNullException">Thrown if the provided term is null.</exception>
	public TermNode(string term, bool isRequired = false)
	{
		if (term is null) throw new ArgumentNullException(nameof(term), "must have a value.");

		Term = term;
		_isRequired = isRequired;
	}


	/// <inheritdoc/>
	public override Task<T> AcceptAsync(IQueryVisitor<T> visitor)
		=> visitor.VisitAsync(this);
    
	/// <inheritdoc/>
	public bool IsVoid() => string.IsNullOrWhiteSpace(Term);
    
	/// <inheritdoc/>
	public bool IsRequirable() => _isRequired;

	/// <summary>
	/// Used for debugging and visualization purposes, returns the term string contained in this node.
	/// </summary>
	/// <returns>The currently stored term.</returns>
	public override string ToString() => Term;
}
