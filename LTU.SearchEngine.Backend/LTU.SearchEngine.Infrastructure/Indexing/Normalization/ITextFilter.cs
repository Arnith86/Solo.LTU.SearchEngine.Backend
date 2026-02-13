using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{
    /*
     อธิบาย:

    รับ string

    คืน string? (nullable)

    ถ้า return null = คำนี้ต้องถูกทิ้ง
     */
    public interface ITextFilter
    {
        string? Apply(string input);
    }
}
