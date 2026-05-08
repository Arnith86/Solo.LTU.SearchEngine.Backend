using LTU.SearchEngine.Backend.Core.RequestParameters;
using LTU.SearchEngine.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Test;

public class QueryableExtensionPaginationTests
{
	private DbContextOptions<TestDbContext> GetInMemoryOptions()
	{
		return new DbContextOptionsBuilder<TestDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
	}

	[Fact]
	public async Task ToPaginatedResultAsync_ReturnsCorrectPageAndMetaData()
	{
		// Arrange
		int expectedPageNumber = 10;


		using var context = new TestDbContext(GetInMemoryOptions());
		var data = Enumerable.Range(1, 25).Select(i => new TestEntity { Id = i });
		await context.Entities.AddRangeAsync(data);
		await context.SaveChangesAsync();

		var paramsObj = new PaginationRequestParameters
		{
			PageNumber = 2,
			PageSize = 10
		};

		// Act
		var result = await context.Entities.AsQueryable().ToPaginatedResultAsync(paramsObj);

		// Assert
		Assert.Equal(expectedPageNumber, result.Items.Count()); // Page 2 should have 10 items
		Assert.Equal(11, result.Items.First().Id); // First item on page 2 (index 10) should have ID 11
		Assert.Equal(25, result.MetaData.TotalItemCount);
		Assert.Equal(3, result.MetaData.TotalPages); // 25 items / 10 per page = 3 pages
		Assert.True(result.MetaData.HasNext);
		Assert.True(result.MetaData.HasPrevious);
	}

	[Fact]
	public async Task ToPaginatedResultAsync_LastPage_HasCorrectItemCount()
	{
		// Arrange
		using var context = new TestDbContext(GetInMemoryOptions());
		var data = Enumerable.Range(1, 25).Select(i => new TestEntity { Id = i });
		await context.Entities.AddRangeAsync(data);
		await context.SaveChangesAsync();

		var paramsObj = new PaginationRequestParameters { PageNumber = 3, PageSize = 10 };

		// Act
		var result = await context.Entities.AsQueryable().ToPaginatedResultAsync(paramsObj);

		// Assert - last page should have 5 items left
		Assert.Equal(5, result.Items.Count()); 
		Assert.False(result.MetaData.HasNext);
	}

	[Fact]
	public async Task ToPaginatedResultAsync_EmptySource_ReturnsEmptyResult()
	{
		// Arrange
		using var context = new TestDbContext(GetInMemoryOptions());
		var paramsObj = new PaginationRequestParameters { PageNumber = 1, PageSize = 10 };

		// Act
		var result = await context.Entities.AsQueryable().ToPaginatedResultAsync(paramsObj);

		// Assert
		Assert.Empty(result.Items);
		Assert.Equal(0, result.MetaData.TotalItemCount);
		Assert.Equal(0, result.MetaData.TotalPages);
	}

	// Helper classes to simulate database environment
	public class TestEntity { public int Id { get; set; } }

	public class TestDbContext : DbContext
	{
		public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
		public DbSet<TestEntity> Entities { get; set; } = null!;
	}
}
