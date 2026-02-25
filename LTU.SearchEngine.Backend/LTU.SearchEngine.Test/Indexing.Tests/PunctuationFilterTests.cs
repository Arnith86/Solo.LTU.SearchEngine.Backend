using LTU.SearchEngine.Infrastructure.Indexing.Normalization;
using Xunit;

namespace LTU.SearchEngine.Test.Indexing.Tests
{
    public class PunctuationFilterTests
    {
        private readonly PunctuationFilter _filter = new PunctuationFilter();

        [Fact]
        public void Apply_GivenOnlyPunctuation_ShouldReturnNull()
        {
            Assert.Null(_filter.Apply("!!!"));
            Assert.Null(_filter.Apply("+++"));
            Assert.Null(_filter.Apply("###"));
        }

        [Fact]
        public void Apply_GivenAlphaNumeric_ShouldReturnSameValue()
        {
            Assert.Equal("Running", _filter.Apply("Running"));
        }

        [Fact]
        public void Apply_GivenAlphaNumericWithPunctuation_ShouldRemovePunctuation()
        {
            Assert.Equal("Hello", _filter.Apply("Hello!!!"));
        }

        [Fact]
        public void Apply_GivenCPlusPlus_ShouldPreservePlus()
        {
            Assert.Equal("C++", _filter.Apply("C++"));
        }

        [Fact]
        public void Apply_GivenCSharp_ShouldPreserveHash()
        {
            Assert.Equal("C#", _filter.Apply("C#"));
        }

        [Fact]
        public void Apply_GivenPlusAroundWord_ShouldRemoveLeadingPlusOnly()
        {
            Assert.Equal("C++", _filter.Apply("++C++"));
        }

        [Fact]
        public void Apply_GivenHashBeforeWord_ShouldRemoveLeadingHash()
        {
            Assert.Equal("C#", _filter.Apply("#C#"));
        }
    }
}
