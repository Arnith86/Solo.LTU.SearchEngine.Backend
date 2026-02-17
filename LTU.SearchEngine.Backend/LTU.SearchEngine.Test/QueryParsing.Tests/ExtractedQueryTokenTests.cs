using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class ExtractedQueryTokenTests
{
	[Fact]
	public void Constructor_ValidArguments_SetsPropertiesCorrectly()
	{
		// Arrange
		var tokenType = QueryTokenType.Term;
		var token = "cat";

		// Act
		var result = new ExtractedQueryToken(tokenType, token);

		// Assert
		Assert.Equal(tokenType, result.TokenType);
		Assert.Equal(token, result.Token);
	}

	[Fact]
	public void Constructor_NullToken_ThrowsArgumentNullException()
	{
		// Arrange
		var tokenType = QueryTokenType.Term;

		// Act & Assert
		var exception = Assert.Throws<ArgumentException>(
			() => new ExtractedQueryToken(tokenType, null!)
		);

		Assert.Equal("token", exception.ParamName);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("   ")]
	public void Constructor_EmptyOrWhitespaceToken_ThrowsArgumentException(string token)
	{
		Assert.Throws<ArgumentException>(
			() => new ExtractedQueryToken(QueryTokenType.Term, token)
		);
	}


	[Theory]
	[InlineData(QueryTokenType.Term, "term")]
	[InlineData(QueryTokenType.Phrase, "this term")]
	[InlineData(QueryTokenType.Operator, "AND")]
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
}
