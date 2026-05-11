using LTU.SearchEngine.Backend.Core.Exceptions.SearchQueryExceptions;
using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Test.Core.Tests;

public class SearchQueryRequestParametersTests
{
	[Fact]
	public void Query_SetValidString_ShouldBeStoredAndTrimmed()
	{
		// Arrange
		var sut = new SearchQueryRequestParameters();
		var input = "  dotnet testing  ";
		var expected = "dotnet testing";

		// Act
		sut.Query = input;

		// Assert
		Assert.Equal(expected, sut.Query);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	public void Query_SetNullOrWhiteSpace_ShouldThrowQuerySyntaxException(string invalidInput)
	{
		// Arrange
		var sut = new SearchQueryRequestParameters();

		// Act & Assert
		var exception = Assert.Throws<QuerySyntaxException>(() => sut.Query = invalidInput);
		Assert.Equal("Search query cannot be empty.", exception.Message);
	}

	[Fact]
	public void Query_SetStringOver500Characters_ShouldThrowQuerySyntaxException()
	{
		// Arrange
		var sut = new SearchQueryRequestParameters();
		var longQuery = new string('a', 501);

		// Act & Assert
		var exception = Assert.Throws<QuerySyntaxException>(() => sut.Query = longQuery);
		Assert.Contains("limited to 500 characters", exception.Message);
	}

	[Fact]
	public void Language_DefaultsToSwedish()
	{
		// Arrange & Act
		var sut = new SearchQueryRequestParameters();

		// Assert
		Assert.Equal("sv", sut.Language);
	}

	[Fact]
	public void Language_CanBeUpdated()
	{
		// Arrange
		var sut = new SearchQueryRequestParameters();
		var newLang = "en";

		// Act
		sut.Language = newLang;

		// Assert
		Assert.Equal(newLang, sut.Language);
	}
}
