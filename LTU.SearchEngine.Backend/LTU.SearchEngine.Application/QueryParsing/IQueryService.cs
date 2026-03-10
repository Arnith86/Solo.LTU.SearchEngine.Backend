using LTU.SearchEngine.Backend.Core.Model.DTOs;

namespace LTU.SearchEngine.Application.QueryParsing
{
	public interface IQueryService
	{
		Task<SearchResponseDTO> GetSearchResultsAsync(string rawQuery, int page);
	}
}