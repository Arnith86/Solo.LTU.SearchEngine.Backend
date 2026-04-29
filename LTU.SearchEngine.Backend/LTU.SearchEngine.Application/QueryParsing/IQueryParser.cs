using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application.QueryParsing;

public interface IQueryParser
{
	QueryParsingResult<HashSet<int>, IgnoredTermsDTO> Parse(string rawQuery, string languageCode = "sv");
}