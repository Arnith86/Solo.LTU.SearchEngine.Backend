namespace LTU.SearchEngine.Test.Core.Tests.TextNormalization;

public class HtmlLanguageCodeConverterTests
{
    private readonly IHtmlLanguageCodeConverter _sut;

    public HtmlLanguageCodeConverterTests()
    {
        _sut = new HtmlLanguageCodeConverter();
    }


    [Theory]
    [InlineData("sv", "Swedish")]
    [InlineData("sv-Se", "Swedish")]
    [InlineData("en", "English")]
    [InlineData("en-US", "English")]
    [InlineData("zh-CN", "Chinese (Simplified)")]
    public void Convert_WithValidCode_ShouldReturnCorrectLanguageName(string input, string expected)
    {
        // Arrange 
        // Act 
        string result = _sut.Convert(input);
        // Assert 
        Assert.Equal(result, expected);
    }
   
    [Theory]
    [InlineData("sV", "Swedish")]
    [InlineData("Sv-sE", "Swedish")]
    [InlineData("En", "English")]
    [InlineData("eN-Us", "English")]
    [InlineData("zH-Cn", "Chinese (Simplified)")]
    public void Convert_ShouldBeCaseInsensitive(string input, string expected)
    {
        // Arrange 
        // Act 
        string result = _sut.Convert(input);
        // Assert 
        Assert.Equal(result, expected);
    }

    [Theory]
    [InlineData("NULL_TEST")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xyz-123")]
    public void Convert_WithInvalidOrUnknownCode_ShouldReturnUnknown(string input)
    {
        // Arrange 
        string? htmlLanguageCode = input.Equals("NULL_TEST") ? null : input;

        // Act
        var result = _sut.Convert(input);

        // Assert
        Assert.Equal("Unknown", result);
    }
}