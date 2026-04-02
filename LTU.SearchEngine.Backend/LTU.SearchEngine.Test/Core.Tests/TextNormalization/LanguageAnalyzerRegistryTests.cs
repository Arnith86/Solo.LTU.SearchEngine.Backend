using LTU.SearchEngine.Backend.Core.TextNormalization;
using Lucene.Net.Analysis;

namespace LTU.SearchEngine.Test.Core.Tests.TextNormalization;

public class LanguageAnalyzerRegistryTests
{
    private readonly ILanguageAnalyzerRegistry _sut;

    public LanguageAnalyzerRegistryTests()
    {
        _sut = new LanguageAnalyzerRegistry();
    }


    // Expand this with new languages when introduced 
    [Theory]
    [InlineData("Swedish")]
    [InlineData("English")]
    public void HasAnalyzerForLanguage_WhenLanguageExists_ShouldReturnTrue(string language)
    {
        Assert.True(_sut.HasAnalyzerForLanguage(language));
    }

    [Fact]
    public void HasAnalyzerForLanguage_WhenLanguageMissing_ShouldReturnFalse()
    {
        Assert.False(_sut.HasAnalyzerForLanguage("NotALanguage"));
    }

    [Theory]
    [InlineData("Swedish")]
    [InlineData("English")]
    public void GetAnalyzerForLanguage_WhenRequested_ShouldReturnValidAnalyzer(string input)
    {
        var analyzer = _sut.GetAnalyzerForLanguage(input);
        
        Assert.NotNull(analyzer);
        Assert.IsAssignableFrom<Lucene.Net.Analysis.Analyzer>(analyzer);
    }

        
    [Theory]
    [InlineData("Running", "run")]
    [InlineData("running", "run")]
    [InlineData("runs", "run")]
    [InlineData("Runs", "run")]
    [InlineData("run's", "run")]
    [InlineData("Run's", "run")]
    [InlineData("Programming", "program")]
    [InlineData("Programs", "program")]
    [InlineData("Swim", "swim")]
    [InlineData("Swimming", "swim")]
    [InlineData("123", "123")]
    [InlineData("C3PO", "c3po")]
    public void EnglishAnalyzer_ShouldStemEnglishCorrectly(string input, string expected)
    {
        // Arrange
        var analyzer = _sut.GetAnalyzerForLanguage("English");
        var term = input; 

        // Act
        var result = Analyze(analyzer, term);

        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Springer", "spring")]
    [InlineData("Spring", "spring")]
    [InlineData("Springare", "spring")]
    [InlineData("Hästar", "häst")]
    [InlineData("Häst", "häst")]
    [InlineData("Hästarna", "häst")]
    [InlineData("123", "123")]
    [InlineData("C3PO", "c3po")]
    public void SwedishAnalyzer_ShouldStemSwedishCorrectly(string input, string expected)
    {
        // Arrange
        var analyzer = _sut.GetAnalyzerForLanguage("Swedish");
        var term = input; 

        // Act
        var result = Analyze(analyzer, term);

        // Assert
        Assert.Equal(expected, result);
    }


    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Att")]
    [InlineData("kan")]
    [InlineData("Jag")]
    [InlineData("För")]
    [InlineData("Som")]
    public void SwedishAnalyzer_BlockWords_ShouldReturnNull(string input)
    {
        // Arrange
        string? inputWord = input.Equals("NULL_TEST") ? null : input;
        
        var analyzer = _sut.GetAnalyzerForLanguage("Swedish");
        var term = inputWord; 

        // Act
        var result = Analyze(analyzer, term!);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("To")]
    [InlineData("The")]
    [InlineData("Or")]
    [InlineData("In")]
    public void EnglishAnalyzer_BlockWords_ShouldReturnNull(string input)
    {
        // Arrange
        string? inputWord = input.Equals("NULL_TEST") ? null : input;
        
        var analyzer = _sut.GetAnalyzerForLanguage("English");
        var term = inputWord; 

        // Act
        var result = Analyze(analyzer, term!);

        // Assert
        Assert.Null(result);
    }


    private string? Analyze(Analyzer analyzer, string text)
    {
        using var reader = new StringReader(text);
        using var stream = analyzer.GetTokenStream("test", reader);
        var termAttr = stream.GetAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();
        
        stream.Reset();
        stream.IncrementToken();
        var result = termAttr.ToString();
        stream.End();
        
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}