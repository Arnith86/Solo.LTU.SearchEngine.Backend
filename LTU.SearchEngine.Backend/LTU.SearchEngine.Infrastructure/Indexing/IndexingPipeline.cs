using LTU.SearchEngine.Backend.Core.Model;
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
        /// <summary>
        /// Transforms a CrawlResult into an IndexDocument.
        /// 
        /// Steps:
        /// 1. Validate input.
        /// 2. Loop through each IndexedTerm.
        /// 3. Normalize each raw term using ITextNormalizer.
        /// 4. Skip terms that normalize to null.
        /// 5. Aggregate term frequency per TermSource.
        /// 6. Build and return IndexDocument.
        /// </summary>
        private readonly ITextNormalizer _textNormalizer;

        public IndexingPipeline(ITextNormalizer normalizer)
        {
            _textNormalizer = normalizer;
        }
        public virtual IndexDocument Transform(CrawlResult crawlResult)
        {
            if (crawlResult == null) throw new ArgumentNullException(nameof(crawlResult));

            var titleTerms = new Dictionary<string, int>();
            var headerTerms = new Dictionary<string, int>();
            var contentTerms = new Dictionary<string, int>();


            foreach (var indexedTerm in crawlResult.IndexedTerms)
            {
                var normalized = _textNormalizer.Normalize(indexedTerm.Term);

                if (normalized == null)
                    continue;

                Dictionary<string, int> targetDictionary;

                switch (indexedTerm.Source)
                {
                    case TermSource.Title:
                        targetDictionary = titleTerms;
                        break;

                    case TermSource.Header:
                        targetDictionary = headerTerms;
                        break;

                    case TermSource.Body:
                        targetDictionary = contentTerms;
                        break;

                    default:
                        continue;
                }

                if (targetDictionary.ContainsKey(normalized))
                {
                    targetDictionary[normalized]++;
                }
                else
                {
                    targetDictionary[normalized] = 1;
                }
            }


            return BuildIndexDocument(
                crawlResult,
                titleTerms,
                headerTerms,
                contentTerms
                );

        }

        private IndexDocument BuildIndexDocument(
            CrawlResult crawlResult, 
            Dictionary<string, int> titleTerms,
            Dictionary<string, int> headerTerms,
            Dictionary<string, int> contentTerms
            )
        {
            return BuildIndexDocument(
                crawlResult,
                titleTerms,
                headerTerms,
                contentTerms
                );
            
        }
    }
}
