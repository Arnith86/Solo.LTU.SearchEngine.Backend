using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core.Model;
using Microsoft.AspNetCore.Http;
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
        /// Handles search request from user.
        /// </summary>
        /// <param name="query">searchstring (t.ex. "cats AND dogs").</param>
        /// <returns>A list with search results.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchResponseDTO>>> GetSearchResponses([FromQuery]string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            // Calls QueryService true ServiceManager to get results
            var results = await _serviceManager.QueryService.SearchAsync(query);

            if (results == null || !results.Any())
            {
              
                return Ok(new List<SearchResponseDTO>());
            }

            return Ok(results);
        }
    }
}
