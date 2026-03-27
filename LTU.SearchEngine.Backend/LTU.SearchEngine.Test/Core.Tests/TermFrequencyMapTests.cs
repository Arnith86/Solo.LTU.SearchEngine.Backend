
using LTU.SearchEngine.Backend.Core.HelperClasses;

namespace LTU.SearchEngine.Test.Core.Tests;

public class TermFrequencyMapTests
{
    private readonly TermFrequencyMap _sut;

    public TermFrequencyMapTests()
    {
        _sut = new TermFrequencyMap();    
    }
    
    [Fact]
    public void AddTerm_GivenNullTerm_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.AddTerm(null!));
    }

    [Fact]
    public void AddTerm_GivenNewTitleTerm_ShouldStartFrequencyAtOne()
    {
        // Act 
        _sut.AddTerm("c++");

        // Assert 
        Assert.Equal(1, _sut.ToReadOnly()["c++"]);
        Assert.Single(_sut.ToReadOnly());
    }


    [Fact]
    public void AddTerm_GivenExistingTitleTerm_ShouldIncrementFrequency()
    {
        // Act 
        _sut.AddTerm("c++");
        _sut.AddTerm("c++");
        
        // Assert 
        Assert.Equal(2, _sut.ToReadOnly()["c++"]);
        Assert.Single(_sut.ToReadOnly());
    }


    [Fact]
    public void AddTerm_GivenExistingTerm_ShouldIncrementFrequency()
    {
        // Act 
        _sut.AddTerm("run");
        _sut.AddTerm("run");

        // Assert 
        Assert.Equal(2, _sut.ToReadOnly()["run"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddTerm_GivenEmptyOrWhiteSpaceString_ShouldThrowArgumentException(string input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.AddTerm(input));
    }
}
