using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{
    /*เหตุผลที่ใช้ HashSet<string>:

    lookup O(1)

    เร็วที่สุด

    เหมาะกับ stopword check
    */
    public interface IStopWordProvider
    {
        HashSet<string> GetStopWords();
    }

}
