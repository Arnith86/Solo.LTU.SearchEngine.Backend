using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing
{
    /// <summary>
    /// Performs the transformation of crawl results into index-ready documents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The indexing pipeline is a pure transformation component that converts a
    /// <see cref="CrawlResult"/> into an <see cref="IndexDocument"/> by normalizing terms
    /// and calculating term frequency per document field.
    /// </para>
    /// <para>
    /// This class does not make decisions, does not perform persistence, and does not
    /// manage indexing flow. It is invoked by the <see cref="Indexer"/> and returns a
    /// fully constructed index document.
    /// </para>
    /// </remarks>
    public class IndexingPipeline
    {
        public virtual IndexDocument TransForm(CrawlResult crawlResult)
        {
            throw new NotImplementedException();
        }
    }
}
