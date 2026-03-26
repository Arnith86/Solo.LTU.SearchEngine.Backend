using System.Text;
using LTU.SearchEngine.Backend.Core.HelperClasses;

namespace LTU.SearchEngine.Test.Core.Tests;

public class ContentHasherTests
{
    private readonly IContentHasher _sut;
    string _emptyString = string.Empty;

    public ContentHasherTests()
    {
        _sut = new ContentHasher();
    }


    [Fact]
    public void CalculateHash_ContentIsNull_ReturnsEmptyString()
    {
        // Arrange 
        byte[] content = null!;

        // Act
        var result = _sut.CalculateHash(content);

        // Assert
        Assert.Equal(_emptyString, result);
    }

    [Fact]
    public void CalculateHash_ContentIsEmpty_ReturnsEmptyString()
    {
        // Arrange 
        byte[] content = new byte[0];

        // Act
        var result = _sut.CalculateHash(content);

        // Assert
        Assert.Equal(_emptyString, result);
    }

    [Fact]
    public void CalculateHash_TwoSeparateIdenticalInput_ReturnsSameHash()
    {
        // Arrange 
        byte[] content1 = Encoding.UTF8.GetBytes("Test content");
        byte[] content2 = Encoding.UTF8.GetBytes("Test content");

        // Act
        var hash1 = _sut.CalculateHash(content1);
        var hash2 = _sut.CalculateHash(content2);

        // Assert
        Assert.Equal(hash1, hash2);
    }
    
    [Fact]
    public void CalculateHash_TwoSeparateDifferentInput_ReturnsDifferentHash()
    {
        // Arrange 
        byte[] content1 = Encoding.UTF8.GetBytes("Test content 1");
        byte[] content2 = Encoding.UTF8.GetBytes("Test content 2");

        // Act
        var hash1 = _sut.CalculateHash(content1);
        var hash2 = _sut.CalculateHash(content2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }
}