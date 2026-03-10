using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
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
        [HttpGet]
        public async Task<ActionResult<SearchResponseDTO>> GetSearchResponses([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            var resultsList = await _serviceManager.QueryService.SearchAsync(query);

            var dto = new SearchResponseDTO(
                searchResults: resultsList,
                currentPage: 1,
                pageSize: resultsList.Count(),
                totalResults: resultsList.Count(),
                message: resultsList.Any() ? "Success" : "No results found"
            );

            return Ok(dto);
        }
    }
}
