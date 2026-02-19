using LTU.SearchEngine.Infrastructure.Indexing.Normalization;
using Moq;
using System;
using Xunit;

namespace LTU.SearchEngine.Test.Indexing.Tests
{
    public class TextNormalizerTests
    {
        private readonly Mock<ITextFilter> _punctuationMock;
        private readonly Mock<ITextFilter> _luceneMock;
        private readonly TextNormalizer _normalizer;

        public TextNormalizerTests()
        {
            _punctuationMock = new Mock<ITextFilter>();
            _luceneMock = new Mock<ITextFilter>();
            _normalizer = new TextNormalizer(
                _punctuationMock.Object,
                _luceneMock.Object);
        }

        [Fact]
        public void Normalize_GivenPunctuationReturnsNull_ShouldReturnNull_AndNotCallLucene()
        {
            // Arrange
            _punctuationMock
                .Setup(f => f.Apply("!!!"))
                .Returns((string?)null);

            // Act
            var result = _normalizer.Normalize("!!!");

            // Assert
            Assert.Null(result);

            _luceneMock.Verify(
                f => f.Apply(It.IsAny<string>()),
                Times.Never()
            );
        }

        [Fact]
        public void Normalize_GivenBothFiltersReturnValue_ShouldReturnLuceneResult()
        {
            // Arrange
            _punctuationMock
                .Setup(f => f.Apply("Running!!!"))
                .Returns("Running");

            _luceneMock
                .Setup(f => f.Apply("Running"))
                .Returns("run");

            // Act
            var result = _normalizer.Normalize("Running!!!");

            // Assert
            Assert.Equal("run", result);

            _punctuationMock.Verify(
                f => f.Apply("Running!!!"),
                Times.Once()
            );

            _luceneMock.Verify(
                f => f.Apply("Running"),
                Times.Once()
            );
        }

        [Fact]
        public void Normalize_GivenLuceneReturnsNull_ShouldReturnNull()
        {
            // Arrange
            _punctuationMock
                .Setup(f => f.Apply("THE"))
                .Returns("THE");

            _luceneMock
                .Setup(f => f.Apply("THE"))
                .Returns((string?)null);

            // Act
            var result = _normalizer.Normalize("THE");

            // Assert
            Assert.Null(result);

            _punctuationMock.Verify(
                f => f.Apply("THE"),
                Times.Once()
            );

            _luceneMock.Verify(
                f => f.Apply("THE"),
                Times.Once()
            );
        }
    }
}
