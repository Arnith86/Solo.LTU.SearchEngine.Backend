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
        Assert.IsType<string>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("NULL_TEST")]
    public void Apply_RawTerm_WhiteSpaceOrNull_ReturnNull(string input)
    {
        // Arrange
        string? rawTerm = input.Equals("NULL_TEST") ? null : input; 
        
        // Act
        var result = _sut.Apply(rawTerm!);

        // Assert
        Assert.Null(result);
    }

    // [Fact]
    // public void Apply_GivenRunning_ShouldReturnRun()
    // {
    //     var result = _filter.Apply("Running");

    //     Assert.Equal("run", result);
    // }

    // [Fact]
    // public void Apply_GivenStopWord_ShouldReturnNull()
    // {
    //     var result = _filter.Apply("THE");

    //     Assert.Null(result);
    // }

    // [Fact]
    // public void Apply_GivenProgramming_ShouldStemToProgram()
    // {
    //     var result = _filter.Apply("Programming");

    //     Assert.Equal("program", result);
    // }

    // [Fact]
    // public void Apply_GivenEmptyString_ShouldReturnNull()
    // {
    //     Assert.Null(_filter.Apply(""));
    // }

    // [Fact]
    // public void Apply_GivenWhitespace_ShouldReturnNull()
    // {
    //     Assert.Null(_filter.Apply(" "));
    // }

    // [Fact]
    // public void Apply_GivenNull_ShouldReturnNull()
    // {
    //     Assert.Null(_filter.Apply(null!));
    // }

    // [Fact]
    // public void Apply_GivenNumber_ShouldReturnSameNumber()
    // {
    //     Assert.Equal("123", _filter.Apply("123"));
    // }

    // [Fact]
    // public void Apply_GivenAlphaNumeric_ShouldPreserveNumbers()
    // {
    //     Assert.Equal("c3po", _filter.Apply("C3PO"));
    // }
}
