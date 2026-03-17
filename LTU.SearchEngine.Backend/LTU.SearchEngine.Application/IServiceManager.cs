using LTU.SearchEngine.Application.QueryParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Application
{
    /// <summary>
    /// Provides a centralized access point for all application services, ensuring consistent dependency management.
    /// </summary>
    public interface IServiceManager
    {
        /// <summary>
        /// Gets the service responsible for processing search queries and retrieving indexed content.
        /// </summary>
        IQueryService QueryService { get; }
    }
}
