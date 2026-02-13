using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Indexing.Normalization;
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

        /*
         Transform:

        Validate input
        Normalize terms
        Build IndexDocument
        Return result
         */

        private readonly ITextNormalizer _textNormalizer;

        public IndexingPipeline(ITextNormalizer normalizer)
        {
            _textNormalizer = normalizer;
        }
        public virtual IndexDocument Transform(CrawlResult crawlResult)
        {
            if (crawlResult == null) throw new ArgumentNullException(nameof(crawlResult));

            var normalizedTerms = _textNormalizer.Normalize(crawlResult.IndexedTerms);

            return BuildIndexDocument(crawlResult, normalizedTerms);
        }

        private IndexDocument BuildIndexDocument(CrawlResult crawlResult, IEnumerable<IndexedTerm> normalizedTerms)
        {
            var document = new IndexDocument(crawlResult.Url, crawlResult.Url);
            foreach (var term in normalizedTerms)
    {
        switch (term.Source)
        {
            case TermSource.Title:
                AddTerm(document.TitleTerms, term.Term);
                break;

            case TermSource.Header:
                AddTerm(document.HeaderTerms, term.Term);
                break;

            case TermSource.Body:
                AddTerm(document.ContentTerms, term.Term);
                break;
        }
    }
            return document;
        }
    }
}
