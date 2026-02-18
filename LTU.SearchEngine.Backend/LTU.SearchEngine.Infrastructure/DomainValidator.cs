using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure
{
    public class DomainValidator : IDomainValidator
    {
        private readonly CrawlerSettings _settings;

        public DomainValidator(CrawlerSettings settings)
        {
            _settings = settings;
        }
        public bool IsWhitelisted(string url)
        {
            //TODO: implement logic for robothandler later, for now we allow all.
            return true;
        }
    }
}
    
