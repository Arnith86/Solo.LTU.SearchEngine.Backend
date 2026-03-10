using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace LTU.SearchEngine.Api
{

    [ApiController]
    [Route("api/[controller]")]
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
        /// <returns>A <see cref="SearchResponse"/> object containing the matching result items and pagination details.</returns>
        [HttpGet]
        public async Task<ActionResult<SearchResponse>> GetSearchResponses([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            // Calls QueryService true ServiceManager to get results
            var resultsFromService = await _serviceManager.QueryService.SearchAsync(query);

            var response = new SearchResponse(
             searchResults: resultsFromService, 
             currentPage: 1,
             pageSize: resultsFromService.Count(),
             totalResults: resultsFromService.Count(),
             message: "Search completed successfully"
      );

            return Ok(response);
        }
    }
}
