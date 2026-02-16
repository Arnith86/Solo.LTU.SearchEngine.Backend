using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{
    public interface ITextFilter
    {
        string? Apply(string rawTerm);
    }
}
