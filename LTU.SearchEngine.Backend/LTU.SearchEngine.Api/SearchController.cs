using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.RequestParameters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace LTU.SearchEngine.Api
{

    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PublicSearchPolicy")]
    public class SearchController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public SearchController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

		/// <summary>
		/// Executes an advanced search query with support for boolean logic and pagination.
		/// </summary>
		/// <remarks>
		/// Query Syntax Support:                                                                   <br />
		/// - Boolean Operators: `AND`, `OR`, `NOT` (e.g., `cats AND dogs`).                        <br />
		/// - Grouping: Use parentheses to control precedence (e.g., `(cats OR dogs) AND birds`).   <br />
		/// - Phrases: Use quotes for exact matches (e.g., `"hello world"`).                        <br />
		/// - Prefixes: Use `+` for mandatory terms or `-` for exclusion.                           <br />
		///                                                                                         <br />
		/// Language Overrides:                                                                     <br />
		/// You can override the global language for specific terms using a prefix (e.g., `sv:katt en:dog`).
		/// </remarks>
		/// <param name="searchParameters">
		/// Parameters for the search query, including the query string (max 500 chars) 
		/// and the default ISO language code (e.g., 'sv', 'en').
		/// </param>
		/// <param name="paginationParameters">
		/// Parameters for controlling pagination, such as PageNumber and PageSize.
		/// </param>
		[HttpGet]
        [SwaggerOperation(
            Summary = "Search the indexed web pages", 
            Description = "Returns a paginated list of documents matching the boolean query.")
        ]
        [SwaggerResponse(200, "Search successful", typeof(SearchResponseDTO))]
        [SwaggerResponse(400, "Invalid query syntax or query too long")]
        [SwaggerResponse(429, "Rate limit exceeded. Please wait before searching again.")]
        public async Task<ActionResult<SearchResponseDTO>> GetSearchResponses(
            [FromQuery] SearchQueryRequestParameters searchParameters,
            [FromQuery] PaginationRequestParameters paginationParameters
            )
        {
            var responseDto = await _serviceManager.QueryService
                .GetSearchResultsAsync(searchParameters, paginationParameters);

            return Ok(responseDto);
        }
    }
}
