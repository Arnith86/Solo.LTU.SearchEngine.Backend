using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization.Filters
{
    public class StopWordFilter : ITextFilter
    {
        private readonly HashSet<string> _stopwords;
        public StopWordFilter(HashSet<string> stopwords)
        {
            _stopwords = stopwords; 
        }
        public string? Apply(string input)
        {
            if (_stopwords.Contains(input)) return null;

            return input;
        }
    }
}
