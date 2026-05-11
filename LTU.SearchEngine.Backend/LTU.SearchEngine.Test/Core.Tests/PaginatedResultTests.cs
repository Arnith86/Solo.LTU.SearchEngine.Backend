using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Test.Core.Tests;

public class PaginatedResultTests
{
	[Fact]
	public void Constructor_AssignsItemsAndMetaData_Correctly()
	{
		// Arrange
		var items = new List<string> { "Result 1", "Result 2", "Result 3" };
		var metaData = new PaginationMetaData(1, 1, 1, 1);

		// Act
		var sut = new PaginatedResult<string>(items, metaData);

		// Assert
		Assert.Equal(items, sut.Items);
		Assert.Equal(metaData, sut.MetaData);
		Assert.Equal(3, sut.Items.Count());
	}

	[Fact]
	public void GenericType_WorksWithReferenceTypes()
	{
		// Arrange
		var items = new List<TestDataObject> { new() { Id = 1 }, new() { Id = 2 } };
		var metaData = new PaginationMetaData(1, 1, 1, 1);

		// Act
		var sut = new PaginatedResult<TestDataObject>(items, metaData);

		// Assert
		Assert.IsType<PaginatedResult<TestDataObject>>(sut);
		Assert.Equal(2, sut.Items.Count());
		Assert.Contains(sut.Items, x => x.Id == 1);
	}

	[Fact]
	public void GenericType_WorksWithValueTypes()
	{
		// Arrange
		var items = new List<int> { 10, 20, 30 };
		var metaData = new PaginationMetaData(1,1,1,1);

		// Act
		var sut = new PaginatedResult<int>(items, metaData);

		// Assert
		Assert.IsType<PaginatedResult<int>>(sut);
		Assert.Equal(3, sut.Items.Count());
	}

	[Fact]
	public void Items_WhenEmpty_InitializesCorrectly()
	{
		// Arrange
		var items = Enumerable.Empty<string>();
		var metaData = new PaginationMetaData(1,1,1,1);

		// Act
		var sut = new PaginatedResult<string>(items, metaData);

		// Assert
		Assert.Empty(sut.Items);
		Assert.NotNull(sut.MetaData);
	}

	// Simple helper class for testing reference types
	private class TestDataObject
	{
		public int Id { get; set; }
	}
}
