using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Miscellaneous;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{
    public class LuceneAnalyzerFilter : ITextFilter
    {
        private readonly Analyzer _analyzer;

        public LuceneAnalyzerFilter()
        {
            _analyzer = CreatAnalyzer();
        }

        private Analyzer CreatAnalyzer()
        {
            return Analyzer.NewAnonymous((fieldName, reader) =>
            {
                // LowerCase term
                var tokenizer = new KeywordTokenizer(reader);
                TokenStream stream = new LowerCaseFilter(LuceneVersion.LUCENE_48, tokenizer);
                // stopWords
                stream = new StopFilter(LuceneVersion.LUCENE_48, stream, EnglishAnalyzer.DefaultStopSet);
                // Normalize "Running" → "run"
                stream = new PorterStemFilter(stream);

                return new TokenStreamComponents(tokenizer, stream);
            });
        }

        public string? Apply(string rawTerm)
        {
            using var reader = new StringReader(rawTerm);
            using var tokenStream = _analyzer.GetTokenStream("field", reader);

            tokenStream.Reset();

            var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();

            if (tokenStream.IncrementToken())
            {
                var result = termAttr.ToString();
                tokenStream.End();
                return result;
            }

            tokenStream.End();
            return null;
        }

    }
}
