using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.QueryParsing.Tests.ValueObjects;

public class SearchResultItemTests
{
	[Fact]
	public void Constructor_ValidValues_ShouldCreateObject()
	{
		// Arrange
		string title = "Example Title";
		string url = "https://example.com";
		string snippet = "This is a snippet";

		// Act
		var result = new SearchResultItem(title, url, snippet);

		// Assert
		Assert.Equal(title, result.Title);
		Assert.Equal(url, result.Url);
		Assert.Equal(snippet, result.Snippet);
	}

	[Theory]
	[InlineData("NULL_TEST")]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_InvalidTitle_ShouldThrowArgumentException(string input)
	{
		string? invalidTitle = input.Equals("NULL_TEST") ? null : input;

		// Arrange
		string url = "https://example.com";
		string snippet = "snippet";

		// Act & Assert
		Assert.Throws<ArgumentException>(() =>
			new SearchResultItem(invalidTitle!, url, snippet));
	}

	[Theory]
	[InlineData("NULL_TEST")]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_InvalidUrl_ShouldThrowArgumentException(string input)
	{
		string? invalidUrl = input.Equals("NULL_TEST") ? null : input;
		
		// Arrange
		string title = "title";
		string snippet = "snippet";

		// Act & Assert
		Assert.Throws<ArgumentException>(() =>
			new SearchResultItem(title, invalidUrl!, snippet));
	}

	[Theory]
	[InlineData("NULL_TEST")]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_InvalidSnippet_ShouldThrowArgumentException(string input)
	{
		string? invalidSnippet = input.Equals("NULL_TEST") ? null : input;
		
		// Arrange
		string title = "title";
		string url = "https://example.com";

		// Act & Assert
		Assert.Throws<ArgumentException>(() =>
			new SearchResultItem(title, url, invalidSnippet!));
	}
}
