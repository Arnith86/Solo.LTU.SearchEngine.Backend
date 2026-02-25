using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Backend.Core.Model
{
    public interface IIndexingPipeline
    {
        IndexDocument Transform(CrawlResult crawlResult);
    }
}
