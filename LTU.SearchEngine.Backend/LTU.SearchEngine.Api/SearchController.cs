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
        /// Executes a search query and returns a response containing results and metadata.
        /// </summary>
        /// <param name="query">The search string to process (e.g., "cats AND dogs").</param>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Perform a search on the search engine.",
            Description = "Retrieves a list of indexed pages based on the provided search query."
        )]
        [SwaggerResponse(200, "The search was successful.", typeof(SearchResponseDTO))]
        [SwaggerResponse(400, "The search query was empty or invalid.")]
        public async Task<ActionResult<SearchResponseDTO>> GetSearchResponses(
            [FromQuery] string query,
            [FromQuery] string language = "sv")
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            var responseDto = await _serviceManager.QueryService.GetSearchResultsAsync(query, language);

            return Ok(responseDto);
        }
    }
}
