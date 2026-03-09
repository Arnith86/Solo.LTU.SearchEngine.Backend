using LTU.SearchEngine.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LTU.SearchEngine.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public SearchController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// Hanterar sökförfrågningar från användaren.
        /// </summary>
        /// <param name="query">Söksträngen (t.ex. "cats AND dogs").</param>
        /// <returns>En lista med sökresultat.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchResponseDTO>>> GetSearchResponses([FromQuery]string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            // Anropar QueryService via ServiceManager för att hämta resultat
            var results = await _serviceManager.QueryService.SearchAsync(query);

            if (results == null || !results.Any())
            {
                // Enligt FRQ-4003 ska UI visa att inga resultat hittades
                return Ok(new List<SearchResponseDTO>());
            }

            return Ok(results);
        }
    }
}
