namespace LTU.SearchEngine.Application.Search.QueryParsing;

public interface IQueryParser
{
    ParsedQuery Parse(string rawQuery);
}
