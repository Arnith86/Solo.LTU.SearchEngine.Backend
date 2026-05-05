using LTU.SearchEngine.Backend.Core.TextNormalization;
using Xunit;

namespace LTU.SearchEngine.Test.Core.Tests.TextNormalization
{
    public class PunctuationFilterTests
    {
        private readonly NoiseFilter _filter = new NoiseFilter();

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

        [Theory]
        [InlineData("A++", "A++")]
        [InlineData("C#", "C#")]
        [InlineData("B!", "B!")]
        [InlineData("!D!", "!D!")]
        [InlineData("#E!", "#E!")]
        [InlineData("-F+", "-F+")]
        public void Apply_GivenCPlusPlus_ShouldPreservePlus(string input, string expected)
        {
            Assert.Equal(expected, _filter.Apply(input));
        }
    }
}
