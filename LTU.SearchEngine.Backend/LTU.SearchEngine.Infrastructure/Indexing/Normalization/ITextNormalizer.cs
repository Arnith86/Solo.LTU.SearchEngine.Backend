using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{
    /*
     หน้าที่:

    รับคำ

    เรียก filters ตามลำดับ

    ถ้า filter ใด return null → หยุดทันที
  
  */
    public interface ITextNormalizer
    {
        string? Normalize(string term);
    }
}
