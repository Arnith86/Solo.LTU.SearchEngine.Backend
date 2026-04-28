using LTU.SearchEngine.Api.ExtensionsUseExceptionHandler.CustomExceptions;
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
	public async Task<HashSet<int>> VisitAsync(TermNode<HashSet<int>> node)
	{
		try
		{
			return await _indexRepository
				.GetDocumentIdsForTermAsync(node.Term);
		}
		catch (Exception ex)
		{
			throw new QueryEvaluationException($"Failed evaluating term '{node.Term}'", ex);
		}
	}

	/// <inheritdoc/>
	public async Task<HashSet<int>> VisitAsync(PhraseNode<HashSet<int>> node)
	{
		try
		{
			return await _indexRepository
				.GetDocumentIdsForPhraseAsync(node);
		}
		catch (Exception ex)
		{
			throw new QueryEvaluationException($"Failed evaluating phrase '{node.Phrase}'", ex);
		}
	}

	/// <inheritdoc/>
	public async Task<HashSet<int>> VisitAsync(LogicOperationNode<HashSet<int>> node)
    {
        bool leftNodeIsVoid = IsNodeVoid(node.LeftNode);
        bool rightNodeIsVoid = IsNodeVoid(node.RightNode);

		if (IsWholeOperationVoid(leftNodeIsVoid, rightNodeIsVoid, node)) return new HashSet<int>();
      	if (leftNodeIsVoid) return await node.RightNode.AcceptAsync(this);
        if (rightNodeIsVoid) return await node.LeftNode.AcceptAsync(this);


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

	private bool IsWholeOperationVoid(bool leftNodeIsVoid, bool rightNodeIsVoid, LogicOperationNode<HashSet<int>> node)
	{
		if (leftNodeIsVoid && rightNodeIsVoid) return true;
        if (IsRequiredPositiveLeftValueOnNotBroken(node, leftNodeIsVoid)) return true;

		return false; 
	}

    private static bool IsRequiredPositiveLeftValueOnNotBroken(LogicOperationNode<HashSet<int>> node, bool leftNodeIsVoid)
   		=> node.LogicalOperator == LogicalOperators.NOT && leftNodeIsVoid;
    

    private static bool IsNodeVoid(QueryNode<HashSet<int>> node)
    {
        if (node is IIsVoidable)
        {
            var isVoidable = node as IIsVoidable;
            if (isVoidable!.IsVoid()) return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public Task<HashSet<int>> VisitAsync(RequiredNode<HashSet<int>> node)
	{
		throw new NotImplementedException();
	}
}
