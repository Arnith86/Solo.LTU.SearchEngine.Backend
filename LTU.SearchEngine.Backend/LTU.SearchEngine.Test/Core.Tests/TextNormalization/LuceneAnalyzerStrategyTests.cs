using LTU.SearchEngine.Backend.Core.TextNormalization;
using Lucene.Net.Analysis.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace LTU.SearchEngine.Test.Core.Tests.TextNormalization;

public class LuceneAnalyzerStrategyTests
{
    private readonly Mock<ILanguageAnalyzerRegistry> _languageAnalyzerRegistryMock;
    private readonly Mock<IHtmlLanguageCodeConverter> _htmlLanguageCodeConverterMock;
    private readonly Mock<ILogger<LuceneAnalyzerStrategy>> _logger;
    private readonly LuceneAnalyzerStrategy _sut;

    public LuceneAnalyzerStrategyTests()
    {
        _languageAnalyzerRegistryMock = new Mock<ILanguageAnalyzerRegistry>();
        _htmlLanguageCodeConverterMock = new Mock<IHtmlLanguageCodeConverter>();
        _logger = new Mock<ILogger<LuceneAnalyzerStrategy>>();

        _sut = new LuceneAnalyzerStrategy(
            _languageAnalyzerRegistryMock.Object,
            _htmlLanguageCodeConverterMock.Object,
            _logger.Object// new Mock<ILogger<LuceneAnalyzerStrategy>>().Object
        );
    }


    [Fact]
    public void GetAppropriateAnalyzer_WhenLanguageExists_ShouldReturnCorrectAnalyzer()
    {
        // Arrange
        string htmlCode = "en-Us";
        string convertedCode = "English";
        var expectedAnalyzer = new KeywordAnalyzer();

        _htmlLanguageCodeConverterMock
            .Setup(h => h.Convert(htmlCode))
            .Returns(convertedCode);

        _languageAnalyzerRegistryMock
            .Setup(l => l.HasAnalyzerForLanguage(convertedCode))
            .Returns(true);

        _languageAnalyzerRegistryMock
            .Setup(l => l.GetAnalyzerForLanguage(convertedCode))
            .Returns(expectedAnalyzer);
        
        // Act 
        var result = _sut.GetAppropriateAnalyzer(htmlCode);

        // Assert
        Assert.Same(expectedAnalyzer, result);
    }

    [Fact]
    public void GetAppropriateAnalyzer_WhenLanguageMissing_ShouldFallbackToSwedish()
    {
        // Arrange
        string htmlCode = "";
        string convertedCode = "Unknown";
        var expectedAnalyzer = new KeywordAnalyzer();

        _htmlLanguageCodeConverterMock
            .Setup(h => h.Convert(htmlCode))
            .Returns(convertedCode);

        _languageAnalyzerRegistryMock
            .Setup(l => l.HasAnalyzerForLanguage(convertedCode))
            .Returns(false);

        _languageAnalyzerRegistryMock
            .Setup(l => l.GetAnalyzerForLanguage("Swedish"))
            .Returns(expectedAnalyzer);
        
        // Act 
        var result = _sut.GetAppropriateAnalyzer(htmlCode);

        // Assert
        Assert.Same(expectedAnalyzer, result);
    }

    [Fact]
    public void GetAppropriateAnalyzer_WhenFallbackOccurs_ShouldLogWarning()
    {
        // Arrange
        string htmlCode = "";
        string convertedCode = "Unknown";
        var expectedAnalyzer = new KeywordAnalyzer();

        _htmlLanguageCodeConverterMock
            .Setup(h => h.Convert(htmlCode))
            .Returns(convertedCode);

        _languageAnalyzerRegistryMock
            .Setup(l => l.HasAnalyzerForLanguage(convertedCode))
            .Returns(false);

        _languageAnalyzerRegistryMock
            .Setup(l => l.GetAnalyzerForLanguage("Swedish"))
            .Returns(expectedAnalyzer);
        
        // Act 
        var result = _sut.GetAppropriateAnalyzer(htmlCode);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No analyzer found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!
            ),
        Times.Once);
    }

    [Fact]
    public void Constructor_WhenDependenciesAreNull_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new LuceneAnalyzerStrategy(null!, _htmlLanguageCodeConverterMock.Object, _logger.Object)
        );
        
        Assert.Throws<ArgumentNullException>(() => 
            new LuceneAnalyzerStrategy(_languageAnalyzerRegistryMock.Object, null!, _logger.Object)
        );
        
        Assert.Throws<ArgumentNullException>(() =>
            new LuceneAnalyzerStrategy(_languageAnalyzerRegistryMock.Object, _htmlLanguageCodeConverterMock.Object, null!)
        );
    }
}