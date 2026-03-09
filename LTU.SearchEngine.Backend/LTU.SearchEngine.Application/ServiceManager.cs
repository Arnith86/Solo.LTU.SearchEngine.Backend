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
            // Vi använder Lazy för att tjänsten bara ska skapas om den faktiskt anropas
            _queryService = new Lazy<IQueryService>(() => queryService);
        }

        public IQueryService QueryService => _queryService.Value;
    }
}
