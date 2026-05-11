using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Test.Core.Tests;

public class PaginationMetaDataTests
{
	[Fact]
	public void Constructor_SetsAllPropertiesCorrectly()
	{
		// Arrange
		int expectedPage = 2;
		int expectedTotalPages = 10;
		int expectedSize = 20;
		int expectedTotalItems = 200;

		// Act
		var sut = new PaginationMetaData(expectedPage, expectedTotalPages, expectedSize, expectedTotalItems);

		// Assert
		Assert.Equal(expectedPage, sut.CurrentPage);
		Assert.Equal(expectedTotalPages, sut.TotalPages);
		Assert.Equal(expectedSize, sut.PageSize);
		Assert.Equal(expectedTotalItems, sut.TotalItemCount);
	}

	[Theory]
	[InlineData(1, 5, false)] // First page: No previous
	[InlineData(2, 5, true)]  // Middle page: Has previous
	[InlineData(5, 5, true)]  // Last page: Has previous
	public void HasPrevious_ReflectsLogicCorrectly(int current, int total, bool expected)
	{
		// Arrange
		var sut = new PaginationMetaData(current, total, 10, 50);

		// Assert
		Assert.Equal(expected, sut.HasPrevious);
	}

	[Theory]
	[InlineData(1, 5, true)]  // First page: Has next
	[InlineData(3, 5, true)]  // Middle page: Has next
	[InlineData(5, 5, false)] // Last page: No next
	[InlineData(6, 5, false)] // Out of bounds: No next
	public void HasNext_ReflectsLogicCorrectly(int current, int total, bool expected)
	{
		// Arrange
		var sut = new PaginationMetaData(current, total, 10, 50);

		// Assert
		Assert.Equal(expected, sut.HasNext);
	}

	[Fact]
	public void Pagination_WithSinglePage_HasNoNavigation()
	{
		// Arrange
		var sut = new PaginationMetaData(1, 1, 10, 5);

		// Assert
		Assert.False(sut.HasPrevious);
		Assert.False(sut.HasNext);
	}

	[Fact]
	public void Pagination_WithZeroItems_HasNoNavigation()
	{
		// Arrange
		var sut = new PaginationMetaData(1, 0, 10, 0);

		// Assert
		Assert.False(sut.HasPrevious);
		Assert.False(sut.HasNext);
	}
}
