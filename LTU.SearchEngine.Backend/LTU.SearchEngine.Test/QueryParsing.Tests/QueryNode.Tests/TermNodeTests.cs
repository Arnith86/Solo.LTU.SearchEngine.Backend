using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests.QueryNode.Tests;

public class TermNodeTests
{
	[Fact]
	public void Constructor_ValidTerm_SetsProperty()
	{
		// Arrange
		var expectedTerm = "apple";

		// Act
		var node = new TermNode<string>(expectedTerm);

		// Assert
		Assert.Equal(expectedTerm, node.Term);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public void Constructor_InvalidTerm_ThrowsArgumentNullException(string invalidTerm)
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(
			() => new TermNode<string>(invalidTerm)
		);
	}


	[Fact]
	public async Task Accept_CallsVisitOnVisitor_ReturnsExpectedValueAsync()
	{
		// Arrange
		var node = new TermNode<string>("apple");
		var mockVisitor = new Mock<IQueryVisitor<string>>();
		var expectedResult = "visited apple";

		mockVisitor
			.Setup(v => v.Visit(node))
			.ReturnsAsync(expectedResult);

		// Act
		var result = await node.Accept(mockVisitor.Object);

		// Assert
		Assert.Equal(expectedResult, result);
		mockVisitor.Verify(v => v.Visit(node), Times.Once);
	}
}
