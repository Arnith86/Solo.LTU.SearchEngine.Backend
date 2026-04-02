using LTU.SearchEngine.Backend.Core.TextNormalization;

namespace LTU.SearchEngine.Test.Core.Tests.TextNormalization;

public class LuceneAnalyzerFilterTests
{
    private readonly LuceneAnalyzerFilter _filter = new LuceneAnalyzerFilter();

    [Fact]
    public void Apply_GivenRunning_ShouldReturnRun()
    {
        var result = _filter.Apply("Running");

        Assert.Equal("run", result);
    }

    [Fact]
    public void Apply_GivenStopWord_ShouldReturnNull()
    {
        var result = _filter.Apply("THE");

        Assert.Null(result);
    }

    [Fact]
    public void Apply_GivenProgramming_ShouldStemToProgram()
    {
        var result = _filter.Apply("Programming");

        Assert.Equal("program", result);
    }

    [Fact]
    public void Apply_GivenEmptyString_ShouldReturnNull()
    {
        Assert.Null(_filter.Apply(""));
    }

    [Fact]
    public void Apply_GivenWhitespace_ShouldReturnNull()
    {
        Assert.Null(_filter.Apply(" "));
    }

    [Fact]
    public void Apply_GivenNull_ShouldReturnNull()
    {
        Assert.Null(_filter.Apply(null!));
    }

    [Fact]
    public void Apply_GivenNumber_ShouldReturnSameNumber()
    {
        Assert.Equal("123", _filter.Apply("123"));
    }

    [Fact]
    public void Apply_GivenAlphaNumeric_ShouldPreserveNumbers()
    {
        Assert.Equal("c3po", _filter.Apply("C3PO"));
    }
}
