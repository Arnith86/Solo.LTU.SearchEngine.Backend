using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Application
{
    public interface IQueryService
    {
        Task<IEnumerable<SearchResponseDTO>> SearchAsync(string query);
    }
}
