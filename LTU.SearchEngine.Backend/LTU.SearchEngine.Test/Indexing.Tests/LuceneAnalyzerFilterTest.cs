using LTU.SearchEngine.Infrastructure.Indexing.Normalization;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Test.Indexing.Tests
{
    public class LuceneAnalyzerFilterTest 
    {

        [Fact]
        public void LuceneAnalyzerFilter_Should_Normalize_Correctly()
        {
            var filter = new LuceneAnalyzerFilter();

            Assert.Equal("run", filter.Apply("Running"));
            Assert.Null(filter.Apply("THE"));
            Assert.Equal("c++", filter.Apply("C++"));
            Assert.Equal("program", filter.Apply("Programming"));
        }

    }
}
