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

        [Fact]
        public void Normalize_ShouldConvertTermToLowercase()
        {
            //Arrange
            var token = new ExtractedQueryToken(QueryTokenType.Term, "CAT");

            // Act
            var result = _normalizer.Normalize(token);

            // Assert
            Assert.Equal("cat", result);
        }

        [Fact]
        public void Normalize_ShouldConvertPhraseToLowercase()
        {
            //Arrange
            var phrase = new ExtractedQueryToken(QueryTokenType.Phrase, "\"Black CAT\"");

            //Act
            var result = _normalizer.Normalize(phrase);

            //Assert
            Assert.Equal("\"black cat\"", result);
        }

        [Fact]
        public void Normalize_ShouldIgnoreLogicalOperators()
        {
            //Arrange
            var token = new ExtractedQueryToken(QueryTokenType.LogicalOperator, "AND");

            // Act
            var result = _normalizer.Normalize(token);

            // Assert
            Assert.Equal("AND", result);
        }

        [Fact]
        public void Normalize_ShouldHandleNullGracefully()
        {
            // Act
            var result = _normalizer.Normalize(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
