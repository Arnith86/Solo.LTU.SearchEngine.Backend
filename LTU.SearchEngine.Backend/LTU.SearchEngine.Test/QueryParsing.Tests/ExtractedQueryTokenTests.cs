using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class ExtractedQueryTokenTests
{
	[Theory]
	[InlineData(QueryTokenType.Term, "term")]
	[InlineData(QueryTokenType.Phrase, "this term")]
	[InlineData(QueryTokenType.LogicalOperator, "AND")]
	[InlineData(QueryTokenType.LogicalOperator, "&&")]
	[InlineData(QueryTokenType.LogicalOperator, "-")]
	[InlineData(QueryTokenType.GroupingOperator, "(")]
	public void Constructor_ValidArguments_SetsPropertiesCorrectly(
		QueryTokenType type, string tokenIn
		)
	{
		// Arrange
		var tokenType = type;
		var token = tokenIn;

		// Act
		var result = new ExtractedQueryToken(tokenType, token);

		// Assert
		Assert.Equal(tokenType, result.TokenType);
		Assert.Equal(token, result.Token);
	}


	[Theory]
	[InlineData(QueryTokenType.GroupingOperator, "((")]
	[InlineData(QueryTokenType.GroupingOperator, "{{")]
	[InlineData(QueryTokenType.GroupingOperator, "[[")]
	[InlineData(QueryTokenType.GroupingOperator, "AA")]
	[InlineData(QueryTokenType.LogicalOperator, "&&&&")]
	[InlineData(QueryTokenType.LogicalOperator, "||||")]
	[InlineData(QueryTokenType.LogicalOperator, "AAAA")]
	public void Constructor_WrongLengthInput_ThrowsArgumentOutOfRangeException(
		QueryTokenType type ,string token
		)
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new ExtractedQueryToken(type, token)
		);
	}


	[Theory]
	[InlineData(QueryTokenType.Term, "term")]
	[InlineData(QueryTokenType.Phrase, "this term")]
	[InlineData(QueryTokenType.LogicalOperator, "AND")]
	public void Constructor_AllTokenTypes_AreStoredCorrectly(
		QueryTokenType tokenType, string inToken
		)
	{
		// Arrange
		var token = inToken;

		// Act
		var result = new ExtractedQueryToken(tokenType, token);

		// Assert
		Assert.Equal(tokenType, result.TokenType);
	}

	[Fact]
    public void Constructor_WithLanguage_SetsLanguageProperty()
    {
        // Arrange
        var token = "search";
        var type = QueryTokenType.Term;
        var language = "en";

        // Act
        var result = new ExtractedQueryToken(type, token, language);

        // Assert
        Assert.Equal(language, result.Language);
    }

    [Fact]
    public void Constructor_DefaultLanguage_IsSwedish()
    {
        // Act
        var result = new ExtractedQueryToken(QueryTokenType.Term, "test");

        // Assert
        Assert.Equal("sv", result.Language);
    }

    [Theory]
    [InlineData(QueryTokenType.LogicalOperator, "AND")] // 3 chars - OK
    [InlineData(QueryTokenType.LogicalOperator, "NOT")] // 3 chars - OK
    [InlineData(QueryTokenType.LogicalOperator, "OR")]  // 2 chars - OK
    [InlineData(QueryTokenType.LogicalOperator, "&&")]  // 2 chars - OK
    [InlineData(QueryTokenType.LogicalOperator, "+")]   // 1 char - OK
    public void Constructor_LogicalOperatorLengthBoundaries_AllowCorrectLengths(QueryTokenType type, string token)
    {
        // Act
        var result = new ExtractedQueryToken(type, token);

        // Assert
        Assert.Equal(token, result.Token);
    }

    [Theory]
    [InlineData(QueryTokenType.GroupingOperator, "")] // 0 chars is technically > 1 false
    [InlineData(QueryTokenType.Term, "VeryLongTermThatShouldBeAllowedBecauseItIsNotAnOperator")]
    public void Constructor_NonOperatorTypes_IgnoreLengthRestrictions(QueryTokenType type, string token)
    {
        // Act
        var result = new ExtractedQueryToken(type, token);

        // Assert
        Assert.Equal(token, result.Token);
    }
}
