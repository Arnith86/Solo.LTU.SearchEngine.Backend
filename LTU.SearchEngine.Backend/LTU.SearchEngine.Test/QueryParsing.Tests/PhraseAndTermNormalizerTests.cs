using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.QueryParsing.Tests
{
    public class PhraseAndTermNormalizerTests
    {
        private readonly PhraseAndTermNormalizer _normalizer;

        public PhraseAndTermNormalizerTests()
        {
            _normalizer = new PhraseAndTermNormalizer();
        }

        [Theory]
        [InlineData("CAT", "cat")]
        [InlineData("C#", "c#")]
        [InlineData("E-POST", "e-post")]
        [InlineData("123-ABC", "123-abc")]
        public void Normalize_ShouldConvertTermsToLowerCaseAndHandleSpecialCharacters(string input, string expected)
        {
            // Arrange
            var token = new ExtractedQueryToken(QueryTokenType.Term, input);

            // Act
            var result = _normalizer.Normalize(token, "en");

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("\"Black CAT\"", "\"black cat\"")]
        [InlineData("\"C# Programming\"", "\"c# programming\"")]
        [InlineData("\"100% Success\"", "\"100% success\"")]
        [InlineData("\"\"", "\"\"")]
        public void Normalize_ShouldConvertPhraseToLowerCaseAndHandleSpecialCharacters(string input, string expected)
        {
            // Arrange
            var token = new ExtractedQueryToken(QueryTokenType.Phrase, input);

            // Act
            var result = _normalizer.Normalize(token, "en");

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("AND")]
        [InlineData("OR")]
        [InlineData("NOT")]
        public void Normalize_ShouldIgnoreLogicalOperators(string op)
        {
            // Arrange
            var token = new ExtractedQueryToken(QueryTokenType.LogicalOperator, op);

            // Act
            var result = _normalizer.Normalize(token, "en");

            // Assert
            Assert.Equal(op, result);
        }

        [Fact]
        public void Normalize_ShouldHandleNullGracefully()
        {
            // Act
            var result = _normalizer.Normalize(null!, "en");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData("(")]
        [InlineData(")")]
        public void Normalize_ShouldNotChangeGroupingOperators(string op)
        {
            // Arrange
            var token = new ExtractedQueryToken(QueryTokenType.GroupingOperator, op);

            // Act
            var result = _normalizer.Normalize(token, "en");

            // Assert
            Assert.Equal(op, result);
        }

        [Fact]
        public void CreatingToken_ShouldThrowException_WhenValueIsEmpty()
        {
            // Arrange
            string emptyValue = "";

            // Act & Assert
            var exception = Assert.Throws<System.ArgumentException>(() =>
                new ExtractedQueryToken(QueryTokenType.Term, emptyValue));

            Assert.Equal("Token cannot be empty. (Parameter 'token')", exception.Message);
        }
    }
}
