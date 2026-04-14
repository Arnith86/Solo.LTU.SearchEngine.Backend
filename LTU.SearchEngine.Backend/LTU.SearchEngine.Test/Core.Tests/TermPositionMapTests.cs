using LTU.SearchEngine.Backend.Core.HelperClasses;

namespace LTU.SearchEngine.Test.HelperClasses;

public class TermPositionMapTests
{
    private readonly TermPositionMap _sut;

    public TermPositionMapTests()
    {
        _sut = new TermPositionMap();
    }

    [Fact]
    public void AddTerm_ShouldStoreTermsInCorrectOrder()
    {
        // Arrange
        var terms = new[] { "small", "mark", "university" };

        // Act
        foreach (var term in terms)
        {
            _sut.AddTerm(term);
        }

        var result = _sut.ToReadOnly();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("small", result[0]);
        Assert.Equal("mark", result[1]);
        Assert.Equal("university", result[2]);
    }

    [Fact]
    public void AddTerm_ShouldAllowDuplicateTerms_AndStorePositions()
    {
        // Arrange & Act
        _sut.AddTerm("hello");
        _sut.AddTerm("world");
        _sut.AddTerm("hello");

        var result = _sut.ToReadOnly();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("hello", result[0]);
        Assert.Equal("hello", result[2]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("NULL_TEST")]
    public void AddTerm_InvalidInput_ShouldThrowException(string input)
    {

        string? invalidTerm = input.Equals("NULL_TEST") ? null : input;

        // Act & Assert
        if (invalidTerm == null)
        {
            Assert.Throws<ArgumentNullException>(() => _sut.AddTerm(invalidTerm!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => _sut.AddTerm(invalidTerm));
        }
    }

    [Fact]
    public void ToReadOnly_ShouldReturnImmutableWrapper()
    {
        // Arrange
        _sut.AddTerm("test");
        var readOnlyList = _sut.ToReadOnly();

        // Act & Assert
        Assert.False(readOnlyList is List<string>);
    }

    [Fact]
    public void AddTerm_AfterToReadOnly_ShouldStillUpdateInternalState()
    {
        // Arrange
        var resultBefore = _sut.ToReadOnly();
        
        // Act
        _sut.AddTerm("new");

        // Assert
        Assert.Contains("new", resultBefore); 
    }
}