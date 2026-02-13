using LTU.SearchEngine.Backend.Core;

namespace LTU.SearchEngine.Application.QueryParsing;

public interface IQueryParser
{
    ParsedQuery Parse(string rawQuery);
}
