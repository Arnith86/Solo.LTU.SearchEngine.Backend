using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization.Filters
{
    public class PunctuationFilter : ITextFilter
    {
        public string? Apply(string rawTerm)
        {
            var builder = new StringBuilder();
            
            bool hasAlphaNumeric = false;

            foreach (var c in rawTerm)
            { 
                if(char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                    hasAlphaNumeric = true;
                } else if (c == '+' || c == '#')
                {
                    builder.Append(c);
                }
                
            }
            if (!hasAlphaNumeric) return null;

            var result = builder.ToString();
            result = result.Trim('+','#');
            if (result.Length == 0) return null;

            return result;
        }
    }
}
