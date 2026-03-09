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

	[Fact]
	public void Constructor_CurrentPageLessThanOne_ShouldThrowArgumentOutOfRangeException()
	{
		// Arrange
		var results = CreateValidResults();

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new SearchResponse(
				results,
				currentPage: 0,
				pageSize: 10,
				totalResults: 1,
				message: "msg"
			)
		);
	}

	[Fact]
	public void Constructor_PageSizeNegative_ShouldThrowArgumentOutOfRangeException()
	{
		// Arrange
		var results = CreateValidResults();

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new SearchResponse(
				results,
				currentPage: 1,
				pageSize: -1,
				totalResults: 1,
				message: "msg"
			)
		);
	}

	[Fact]
	public void Constructor_TotalResultsNegative_ShouldThrowArgumentOutOfRangeException()
	{
		// Arrange
		var results = CreateValidResults();

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new SearchResponse(
				results,
				currentPage: 1,
				pageSize: 10,
				totalResults: -5,
				message: "msg"
			)
		);
	}
}
