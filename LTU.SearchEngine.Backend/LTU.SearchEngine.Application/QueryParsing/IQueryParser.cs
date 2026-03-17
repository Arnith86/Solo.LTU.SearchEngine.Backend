using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Application.QueryParsing
{
	public interface IQueryParser
	{
		QueryNode<HashSet<int>> Parse(string rawQuery);
	}
}