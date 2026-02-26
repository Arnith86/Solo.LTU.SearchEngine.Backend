using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Backend.Core;

/// <summary>
/// Defines a generic Visitor for traversing and processing the Search Engine's Abstract Syntax Tree (AST).
/// </summary>
/// <typeparam name="TResult">The type of the result produced by visiting a node or Expression).</typeparam>
/// <remarks>
/// This interface is the core of the Visitor Pattern implementation, allowing new operations 
/// (like SQL generation, result fetching, or query validation) to be added without modifying the <see cref="QueryNode{T}"/> classes.
/// </remarks>
public interface IQueryVisitor<TResult>
{
	/// <summary>Processes a single-term leaf node.</summary>
	/// <param name="node">The node containing the search term.</param>
	/// <returns>The result of the operation on the term node.</returns>
	TResult Visit(TermNode<TResult> node);

	/// <summary>Processes a quoted phrase leaf node.</summary>
	/// <param name="node">The node containing the exact search phrase.</param>
	/// <returns>The result of the operation on the phrase node.</returns>
	TResult Visit(PhraseNode<TResult> node);
}