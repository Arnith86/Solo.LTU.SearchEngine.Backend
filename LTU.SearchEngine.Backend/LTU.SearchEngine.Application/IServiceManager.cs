using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Application
{
    public interface IServiceManager
    {
        // En "huvudnyckel" som ger tillgång till sök-tjänsten
        IQueryService QueryService { get; }
    }
}
