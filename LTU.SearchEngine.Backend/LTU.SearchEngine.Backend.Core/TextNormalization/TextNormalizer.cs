using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Backend.Core.TextNormalization
{
    public class TextNormalizer : ITextNormalizer<string>
    {
        private readonly ITextFilter _punctuationFilter;
        private readonly ITextFilter _luceneFilter;

        public TextNormalizer(ITextFilter punctuationFilter, ITextFilter luceneFilter)
        {
            _punctuationFilter = punctuationFilter;
            _luceneFilter = luceneFilter;

        }

        public string? Normalize(string rawTerm)
        {
            var cleaned = _punctuationFilter.Apply(rawTerm);

            if (cleaned == null) return null;

            return _luceneFilter.Apply(cleaned);
        }
    }
}
