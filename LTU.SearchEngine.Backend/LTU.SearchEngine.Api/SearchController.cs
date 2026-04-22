using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
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
        /// Executes a advanced search query.
        /// </summary>
        /// <remarks>
        /// Supports boolean logic:
        /// - AND: `cats AND dogs`  <br/>
        /// - OR: `cats OR dogs`    <br/>
        /// - NOT: `cats NOT dogs`  <br/>
        /// - Grouping: `(cats AND dogs) OR birds` <br/>
        /// </remarks>
        /// <param name="query">The search string (Max 500 chars).</param>
        /// <param name="page">The page number (1-based index). Defaults to 1.</param>
        /// <param name="language">ISO language code (e.g., 'sv', 'en'). Defaults to 'sv'.</param>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Search the indexed web pages", 
            Description = "Returns a paginated list of documents matching the boolean query.")
        ]
        [SwaggerResponse(200, "Search successful", typeof(SearchResponseDTO))]
        [SwaggerResponse(400, "Invalid query syntax or query too long")]
        [SwaggerResponse(429, "Rate limit exceeded. Please wait before searching again.")]
        public async Task<ActionResult<SearchResponseDTO>> GetSearchResponses(
            [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] string language = "sv")
        {
            if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty."); 
            if (query.Length > 500) return BadRequest("Query length is limited to 500 characters!");
            if (pageNumber < 1) return BadRequest("Page number must be 1 or greater.");
            if (pageNumber > 100) return BadRequest("Deep pagination is restricted. Please refine your query!");

            var responseDto = await _serviceManager.QueryService.GetSearchResultsAsync(query, language);

            return Ok(responseDto);
        }
    }
}
