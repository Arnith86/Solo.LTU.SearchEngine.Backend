using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Test.Core.Tests;

public class PaginationRequestParametersTests
{
	[Fact]
	public void PageNumber_DefaultsToOne()
	{
		// Arrange & Act
		var sut = new PaginationRequestParameters();

		// Assert
		Assert.Equal(1, sut.PageNumber);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-50)]
	public void PageNumber_SetToLessThanOne_CorrectsToOne(int invalidPage)
	{
		// Arrange
		var sut = new PaginationRequestParameters();

		// Act
		sut.PageNumber = invalidPage;

		// Assert
		Assert.Equal(1, sut.PageNumber);
	}

	[Fact]
	public void PageNumber_SetAboveOneHundred_ThrowsPaginationOutOfRangeException()
	{
		// Arrange
		var sut = new PaginationRequestParameters();

		// Act & Assert
		var exception = Assert.Throws<PaginationOutOfRangeException>(() => sut.PageNumber = 101);
		Assert.Contains("Deep pagination is restricted", exception.Message);
	}

	[Fact]
	public void PageSize_DefaultsToTen()
	{
		// Arrange & Act
		var sut = new PaginationRequestParameters();

		// Assert
		Assert.Equal(10, sut.PageSize);
	}

	[Theory]
	[InlineData(101)]
	[InlineData(1000)]
	public void PageSize_SetAboveMaxLimit_CapsAtOneHundred(int oversizedPageSize)
	{
		// Arrange
		var sut = new PaginationRequestParameters();

		// Act
		sut.PageSize = oversizedPageSize;

		// Assert
		Assert.Equal(100, sut.PageSize);
	}

	[Fact]
	public void Deconstruct_ReturnsCorrectValues()
	{
		// Arrange
		var sut = new PaginationRequestParameters
		{
			PageNumber = 5,
			PageSize = 25
		};

		// Act
		var (pageNumber, pageSize) = sut;

		// Assert
		Assert.Equal(5, pageNumber);
		Assert.Equal(25, pageSize);
	}

	[Fact]
	public void PageNumber_SetToValidValue_UpdatesCorrectly()
	{
		// Arrange
		var sut = new PaginationRequestParameters();
		int validPage = 42;

		// Act
		sut.PageNumber = validPage;

		// Assert
		Assert.Equal(validPage, sut.PageNumber);
	}
}
