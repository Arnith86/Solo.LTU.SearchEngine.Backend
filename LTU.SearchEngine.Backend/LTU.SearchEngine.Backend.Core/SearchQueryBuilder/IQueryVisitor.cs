using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

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
	/// <summary>
	/// Initiates the traversal of a query tree starting from the specified root node.
	/// </summary>
	/// <param name="node">The root or sub-root <see cref="QueryNode{TResult}"/> to begin processing from.</param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> representing the asynchronous operation, <br/>
	/// containing the final processed result of the entire query branch.
	/// </returns>
	Task<TResult> ExecuteAsync(QueryNode<TResult> node);

	/// <summary>Processes a single-term leaf node.</summary>
	/// <param name="node">The node containing the search term.</param>
	/// <returns>The result of the operation on the term node.</returns>
	Task<TResult> VisitAsync(TermNode<TResult> node);

	/// <summary>Processes a quoted phrase leaf node.</summary>
	/// <param name="node">The node containing the exact search phrase.</param>
	/// <returns>The result of the operation on the phrase node.</returns>
	Task<TResult> VisitAsync(PhraseNode<TResult> node);

	/// <summary>
	/// Processes a binary logical operation (e.g., AND, OR) or a NOT operation.
	/// </summary>
	/// <param name="node">The node representing the logical connection between two query branches.</param>
	/// <returns>
	/// The combined result of the binary operation. 
	/// If one branch is void, the visitor may return the result of the non-void branch to simplify the execution.
	/// If however, both branches are void, then an empty <see cref="TResult"/> is returned.
	/// </returns> 
	Task<TResult> VisitAsync(LogicOperationNode<TResult> node);

	/// <summary>Processes a node marked with the required operator (+).</summary>
	/// <param name="node">The node that must be present in the search results.</param>
	/// <returns>The result of the operation on the required node.</returns>
	Task<TResult> VisitAsync(RequiredNode<TResult> node);
}