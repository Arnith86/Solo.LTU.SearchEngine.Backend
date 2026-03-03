using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

public interface ITreeBuilder<TResult, TType>
{
	QueryNode<TResult> BuildTree(IEnumerable<TType> tokens);
}