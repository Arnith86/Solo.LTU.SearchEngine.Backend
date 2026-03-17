using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using LTU.SearchEngine.Infrastructure.Repositories;


namespace LTU.SearchEngine.Backend.Core;

public class QueryEvaluatorVisitor : IQueryVisitor<HashSet<int>>
{
	private readonly IIndexRepository _indexRepository;

	public QueryEvaluatorVisitor(IIndexRepository indexRepository)
	{
		_indexRepository = indexRepository ?? 
			throw new ArgumentNullException(nameof(indexRepository), "Cannot be null!");	
	}

	/// <inheritdoc/>
	public async Task<HashSet<int>> ExecuteAsync(QueryNode<HashSet<int>> node)
	{
		var result = await node.AcceptAsync(this);

		// ToDo: At this point, we evaluate against the required nodes. Not implemented yet.

		return result;
	}

	/// <inheritdoc/>
	public async Task<HashSet<int>> VisitAsync(TermNode<HashSet<int>> node) =>
		await _indexRepository
			.GetDocumentIdsForTermAsync(node.Term);

	/// <inheritdoc/>
	public async Task<HashSet<int>> VisitAsync(PhraseNode<HashSet<int>> node) =>
		 await _indexRepository
			.GetDocumentIdsForPhraseAsync(node);

	/// <inheritdoc/>
	public async Task<HashSet<int>> VisitAsync(LogicOperationNode<HashSet<int>> node)
	{
		var left = await node.LeftNode.AcceptAsync(this);
		var right = await node.RightNode.AcceptAsync(this);

		return node.LogicalOperator switch
		{
			LogicalOperators.AND => left.Intersect(right).ToHashSet(),
			LogicalOperators.OR => left.Union(right).ToHashSet(),
			LogicalOperators.NOT => left.Except(right).ToHashSet(),
			_ => throw new InvalidOperationException($"Unsupported logical operator: {node.LogicalOperator}")
		};
	}

	/// <inheritdoc/>
	public Task<HashSet<int>> VisitAsync(RequiredNode<HashSet<int>> node)
	{
		throw new NotImplementedException();
	}
}
