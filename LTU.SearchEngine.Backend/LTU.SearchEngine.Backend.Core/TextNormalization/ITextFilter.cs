using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Backend.Core.TextNormalization
{
    public interface ITextFilter
    {
        string? Apply(string rawTerm);
    }
}
