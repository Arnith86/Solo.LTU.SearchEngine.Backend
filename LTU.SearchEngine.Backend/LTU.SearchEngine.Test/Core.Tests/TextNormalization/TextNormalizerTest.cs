using LTU.SearchEngine.Backend.Core.TextNormalization;
using Moq;

namespace LTU.SearchEngine.Test.Core.Tests.TextNormalization;

public class TextNormalizerTests
{
    private readonly Mock<INoiseFilter> _noiseFilterMock;
    private readonly Mock<ILuceneFilter> _luceneFilterMock;
    private readonly TextNormalizer _sut;

    public TextNormalizerTests()
    {
        _noiseFilterMock = new Mock<INoiseFilter>();
        _luceneFilterMock = new Mock<ILuceneFilter>();
        _sut = new TextNormalizer(
            _noiseFilterMock.Object,
            _luceneFilterMock.Object);
    }

    [Fact]
    public void Normalize_GivenPunctuationReturnsNull_ShouldReturnEmptyList_AndNotCallLucene()
    {
        // Arrange
        _noiseFilterMock
            .Setup(f => f.Apply("!!!", "en"))
            .Returns((string?)null);

        // Act
        var result = _sut.Normalize("!!!", "en");

        // Assert
        Assert.Empty(result);

        _luceneFilterMock.Verify(
            f => f.Apply(It.IsAny<string>()),
            Times.Never()
        );
    }

    [Fact]
    public void Normalize_GivenBothFiltersReturnValue_ShouldReturnLuceneResult()
    {
        // Arrange
        _noiseFilterMock
            .Setup(f => f.Apply("Running!!!", "en"))
            .Returns("Running");

        _luceneFilterMock
            .Setup(f => f.Apply("Running", "en"))
            .Returns(new List<string>{"run"});

        // Act
        var result = _sut.Normalize("Running!!!", "en");

        // Assert
        Assert.Equal("run", result.First());

        _noiseFilterMock.Verify(
            f => f.Apply("Running!!!", "en"),
            Times.Once()
        );

        _luceneFilterMock.Verify(
            f => f.Apply("Running", "en"),
            Times.Once()
        );
    }

    [Fact]
    public void Normalize_GivenLuceneReturnsNull_ShouldReturnEmptyList()
    {
        // Arrange
        _noiseFilterMock
            .Setup(f => f.Apply("THE", "en"))
            .Returns("THE");

        _luceneFilterMock
            .Setup(f => f.Apply("THE", "en"))
            .Returns(new List<string>());

        // Act
        var result = _sut.Normalize("THE", "en");

        // Assert
        Assert.Empty(result);

        _noiseFilterMock.Verify(
            f => f.Apply("THE", "en"),
            Times.Once()
        );

        _luceneFilterMock.Verify(
            f => f.Apply("THE", "en"),
            Times.Once()
        );
    }
    
    [Fact]
    public void Normalize_TechnicalTerm_BypassesLuceneAndReturnsSingleToken()
    {
        // Arrange
        // Note: Lucene filter is NOT set up because it should be bypassed
        var input = "C++";
        _noiseFilterMock
            .Setup(f => f.Apply(input, "en"))
            .Returns("C++");

        // Act
        var result = _sut.Normalize(input, "en").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("c++", result[0]);
        _luceneFilterMock.Verify(f => 
            f.Apply(It.IsAny<string>(), It.IsAny<string>()), 
            Times.Never
        );
    }

    [Fact]
    public void Normalize_LinguisticTermWithHyphen_ExplodesIntoMultipleTokens()
    {
        // Arrange
        var input = "Lars-Åke";
        var luceneOutput = new List<string> { "lars", "åke" };
        
        _noiseFilterMock
            .Setup(f => f.Apply(input, "sv"))
            .Returns("Lars-Åke");

        _luceneFilterMock
            .Setup(f => f.Apply("Lars-Åke", "sv"))
            .Returns(luceneOutput);

        // Act
        var result = _sut.Normalize(input, "sv").ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("lars", result);
        Assert.Contains("åke", result);
        _luceneFilterMock.Verify(f => f.Apply("Lars-Åke", "sv"), Times.Once);
    }

    [Fact]
    public void Normalize_NoiseTerm_ReturnsEmptyCollection()
    {
        // Arrange
        var input = "!!!";
        // Noise filter returns null or empty for invalid terms
        _noiseFilterMock
            .Setup(f => f.Apply(input, "en"))
            .Returns((string) null!);

        // Act
        var result = _sut.Normalize(input, "en");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Normalize_StandardWord_ReturnsStemmedTokenFromLucene()
    {
        // Arrange
        var input = "running";
        _noiseFilterMock
            .Setup(f => f.Apply(input, "en"))
            .Returns("running");

        _luceneFilterMock
            .Setup(f => f.Apply("running", "en"))
            .Returns(new List<string> { "run" });

        // Act
        var result = _sut.Normalize(input, "en").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("run", result[0]);
    }

    [Theory]
    [InlineData("C#", "c#")]
    [InlineData("AT&T", "at&t")]
    [InlineData("Email@Email.com", "email@email.com")]
    public void Normalize_ProtectedSymbols_MaintainIntegrity(string input, string expected)
    {
        // Arrange
        _noiseFilterMock
            .Setup(f => f.Apply(input, "en"))
            .Returns( input );

        // Act
        var result = _sut.Normalize(input, "en");

        // Assert
        Assert.Contains(expected, result);
        _luceneFilterMock.Verify(f => 
            f.Apply(It.IsAny<string>(), It.IsAny<string>()), 
            Times.Never
        );
    }
}
