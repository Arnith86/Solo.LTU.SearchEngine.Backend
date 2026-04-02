using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing
{
    /// <summary>
    /// Defines the core search functionality for the application, handling query parsing and document retrieval.
    /// </summary>
    public interface IQueryService
    {
        /// <summary>
        /// Performs an asynchronous search based on the provided query string.
        /// </summary>
        /// <param name="query">The search expression to evaluate (e.g., "cats AND dogs").</param>
        /// <param name="languageCode">The main language of the given query.</param> 
        /// <returns>A task representing the asynchronous operation, containing a collection of <see cref="SearchResultItem"/>.</returns>
        Task<SearchResponseDTO> GetSearchResultsAsync(string query, string languageCode = "sv");
    }
}
