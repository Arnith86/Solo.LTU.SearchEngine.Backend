using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization.Filters
{
    public class LowerCaseFilter : ITextFilter
    {
        public string? Apply(string input)
        {
            return input.ToLowerInvariant();
        }
    }
}
