using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers
{
    public interface ITextNormalizer<T>
    {
        string Normalize(T token);
    }
}
