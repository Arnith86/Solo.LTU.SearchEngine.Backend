using LTU.SearchEngine.Application.QueryParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Application
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<IQueryService> _queryService;

        public ServiceManager(IQueryService queryService)
        {
            // We use Lazy only for the service to be created if called
            _queryService = new Lazy<IQueryService>(() => queryService);
        }

        public IQueryService QueryService => _queryService.Value;
    }
}
