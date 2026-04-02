using LTU.SearchEngine.Backend.Core.TextNormalization;
using Moq;

namespace LTU.SearchEngine.Test.Core.Tests.TextNormalization;

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
            .Setup(f => f.Apply("!!!", "en"))
            .Returns((string?)null);

        // Act
        var result = _normalizer.Normalize("!!!", "en");

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
            .Setup(f => f.Apply("Running!!!", "en"))
            .Returns("Running");

        _luceneMock
            .Setup(f => f.Apply("Running", "en"))
            .Returns("run");

        // Act
        var result = _normalizer.Normalize("Running!!!", "en");

        // Assert
        Assert.Equal("run", result);

        _punctuationMock.Verify(
            f => f.Apply("Running!!!", "en"),
            Times.Once()
        );

        _luceneMock.Verify(
            f => f.Apply("Running", "en"),
            Times.Once()
        );
    }

    [Fact]
    public void Normalize_GivenLuceneReturnsNull_ShouldReturnNull()
    {
        // Arrange
        _punctuationMock
            .Setup(f => f.Apply("THE", "en"))
            .Returns("THE");

        _luceneMock
            .Setup(f => f.Apply("THE", "en"))
            .Returns((string?)null);

        // Act
        var result = _normalizer.Normalize("THE", "en");

        // Assert
        Assert.Null(result);

        _punctuationMock.Verify(
            f => f.Apply("THE", "en"),
            Times.Once()
        );

        _luceneMock.Verify(
            f => f.Apply("THE", "en"),
            Times.Once()
        );
    }
}
