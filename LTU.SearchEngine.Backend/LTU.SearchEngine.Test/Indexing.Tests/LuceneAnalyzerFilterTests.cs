using LTU.SearchEngine.Infrastructure.Indexing.Normalization;
using Xunit;

namespace LTU.SearchEngine.Test.Indexing.Tests
{
    public class LuceneAnalyzerFilterTests
    {
        private readonly LuceneAnalyzerFilter _filter = new LuceneAnalyzerFilter();

        [Fact]
        public void Apply_GivenRunning_ShouldReturnRun()
        {
            var result = _filter.Apply("Running");

            Assert.Equal("run", result);
        }

        [Fact]
        public void Apply_GivenStopWord_ShouldReturnNull()
        {
            var result = _filter.Apply("THE");

            Assert.Null(result);
        }

        [Fact]
        public void Apply_GivenCPlusPlus_ShouldReturnLowercaseWithoutRemovingSymbols()
        {
            var result = _filter.Apply("C++");

            Assert.Equal("c++", result);
        }

        [Fact]
        public void Apply_GivenProgramming_ShouldStemToProgram()
        {
            var result = _filter.Apply("Programming");

            Assert.Equal("program", result);
        }
    }
}
