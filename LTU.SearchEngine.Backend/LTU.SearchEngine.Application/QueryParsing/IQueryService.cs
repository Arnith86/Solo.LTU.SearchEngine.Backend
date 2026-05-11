using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Application.QueryParsing;

/// <summary>
/// Defines the primary search gateway for the application, responsible for orchestrating 
/// query parsing, logical evaluation, and paginated document retrieval.
/// </summary>
/// <remarks>
/// This service acts as the entry point for the API layer, aggregating the results 
/// from the Query Parser and the Document Repository into a unified response DTO.
/// </remarks>
public interface IQueryService
{
	/// <summary>
	/// Executes a comprehensive search operation asynchronously based on complex query logic and pagination settings.
	/// </summary>
	/// <param name="searchParameters">
	/// Encapsulates the user's search expression and the global language context 
	/// (e.g., query string, default ISO language code).
	/// </param>
	/// <param name="paginationParameters">
	/// Encapsulates settings for controlling the result set window, 
	/// including page numbers and page sizes.
	/// </param> 
	/// <returns>
	/// A task representing the asynchronous operation, containing a <see cref="SearchResponseDTO"/> 
	/// which includes matching documents, pagination metadata, and information about ignored terms.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown if either parameter object is null.</exception>
	/// <exception cref="LTU.SearchEngine.Backend.Core.Exceptions.InvalidQueryStringException">
	/// Thrown if the search expression within <paramref name="searchParameters"/> fails logical validation.
	/// </exception>
	Task<SearchResponseDTO> GetSearchResultsAsync(
        SearchQueryRequestParameters searchParameters,
        PaginationRequestParameters paginationParameters
    );
}
