using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{
    public interface ITextNormalizer
    {
        string? Normalize(string term);
    }
}
