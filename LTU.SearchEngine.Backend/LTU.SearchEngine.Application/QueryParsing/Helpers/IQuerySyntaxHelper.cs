using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// Provides syntax analysis utilities for search queries.
/// </summary>
public interface IQuerySyntaxHelper
{
	void ValidateGrouping(List<ExtractedQueryToken> tokens);
}