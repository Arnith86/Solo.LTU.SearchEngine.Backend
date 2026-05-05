using LTU.SearchEngine.Backend.Core.TextNormalization;
using Lucene.Net.Analysis;
using Microsoft.Extensions.Logging;
using Moq;

namespace LTU.SearchEngine.Test.Core.Tests.TextNormalization;

public class LuceneAnalyzerFilterTests
{
    private readonly Mock<ILanguageAnalyzerRegistry> _languageAnalyzerRegistryMock;
    private readonly Mock<IHtmlLanguageCodeConverter> _htmlLanguageCodeConverterMock;
    private readonly Mock<LuceneAnalyzerStrategy> _luceneAnalyzerStrategyMock;
    private readonly Analyzer _stubAnalyzer; 
    private readonly LuceneAnalyzerFilter _sut;

    public LuceneAnalyzerFilterTests()
    {
        _languageAnalyzerRegistryMock = new Mock<ILanguageAnalyzerRegistry>();
        _htmlLanguageCodeConverterMock = new Mock<IHtmlLanguageCodeConverter>();

        _luceneAnalyzerStrategyMock = new Mock<LuceneAnalyzerStrategy>(
            _languageAnalyzerRegistryMock.Object,
            _htmlLanguageCodeConverterMock.Object,
            new Mock<ILogger<LuceneAnalyzerStrategy>>().Object
        );

        _stubAnalyzer = new Lucene.Net.Analysis.Core.KeywordAnalyzer();

        _sut = new LuceneAnalyzerFilter(_luceneAnalyzerStrategyMock.Object);
    }


    [Theory]
    [InlineData("sv", "Swedish")]
    [InlineData("en", "English")]
    public void Apply_ShouldCallStrategyWithProvidedLanguageCode(string code, string language)
    {
        // Arrange
        _htmlLanguageCodeConverterMock
            .Setup(h => h.Convert(code))
            .Returns(language);

        _luceneAnalyzerStrategyMock
            .Setup(l => l.GetAppropriateAnalyzer(code))
            .Returns(_stubAnalyzer);
  
        
        // Act
        var result = _sut.Apply("test", code)!;

        // Assert
        _luceneAnalyzerStrategyMock.Verify(s => s.GetAppropriateAnalyzer(code), Times.Once);
        Assert.NotNull(result);
        Assert.IsType<List<string>>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("NULL_TEST")]
    public void Apply_RawTerm_WhiteSpaceOrNull_ReturnEmptyList(string input)
    {
        // Arrange
        string? rawTerm = input.Equals("NULL_TEST") ? null : input; 
        
        // Act
        var result = _sut.Apply(rawTerm!);

        // Assert
        Assert.Empty(result);
    }
}
