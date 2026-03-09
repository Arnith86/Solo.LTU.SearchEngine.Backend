using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.QueryParsing.Tests.ValueObjects;

public class SearchResponseTests
{
	private List<SearchResultItem> CreateValidResults()
	{
		return new List<SearchResultItem>
		{
			new SearchResultItem("Title", "https://example.com", "snippet")
		};
	}

	[Fact]
	public void Constructor_ValidArguments_ShouldCreateObject()
	{
		// Arrange
		var results = CreateValidResults();

		// Act
		var sut = new SearchResponse(
			results,
			currentPage: 1,
			pageSize: 10,
			totalResults: 1,
			message: "Success"
		);

		// Assert
		Assert.Equal(results, sut.SearchResults);
		Assert.Equal(1, sut.CurrentPage);
		Assert.Equal(10, sut.PageSize);
		Assert.Equal(1, sut.TotalResults);
		Assert.Equal("Success", sut.Message);
	}

	[Fact]
	public void Constructor_NullSearchResults_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			new SearchResponse(
				null!,
				currentPage: 1,
				pageSize: 10,
				totalResults: 1,
				message: "msg"
			)
		);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-10)]
	public void Constructor_CurrentPageLessThanOne_ShouldThrowArgumentOutOfRangeException(int value)
	{
		// Arrange
		var results = CreateValidResults();

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new SearchResponse(
				results,
				currentPage: value,
				pageSize: 10,
				totalResults: 1,
				message: "msg"
			)
		);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-10)]
	public void Constructor_PageSizeNegative_ShouldThrowArgumentOutOfRangeException(int value)
	{
		// Arrange
		var results = CreateValidResults();

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new SearchResponse(
				results,
				currentPage: 1,
				pageSize: value,
				totalResults: 1,
				message: "msg"
			)
		);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-10)]
	public void Constructor_TotalResultsNegative_ShouldThrowArgumentOutOfRangeException(int value)
	{
		// Arrange
		var results = CreateValidResults();

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new SearchResponse(
				results,
				currentPage: 1,
				pageSize: 10,
				totalResults: value,
				message: "msg"
			)
		);
	}
}
