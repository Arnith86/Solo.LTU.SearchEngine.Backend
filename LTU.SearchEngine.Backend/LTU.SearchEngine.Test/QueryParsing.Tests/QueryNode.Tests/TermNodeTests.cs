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

	[Fact]
	public void Constructor_InvalidTerm_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(
			() => new TermNode<string>(null!)
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
			.Setup(v => v.VisitAsync(node))
			.ReturnsAsync(expectedResult);

		// Act
		var result = await node.AcceptAsync(mockVisitor.Object);

		// Assert
		Assert.Equal(expectedResult, result);
		mockVisitor.Verify(v => v.VisitAsync(node), Times.Once);
	}

	[Theory]
    [InlineData("apple", false)]      // Normal word
    [InlineData("a", false)]          // Single character
    [InlineData("123", false)]        // Numbers
    [InlineData("", true)]            // Empty string
    [InlineData(" ", true)]           // Single space
    [InlineData("\t\n\r", true)]      // Whitespace characters
    public void IsVoid_ReturnsExpectedResult(string inputTerm, bool expectedIsVoid)
    {
        // Arrange
        // Note: Constructor allows empty/whitespace but not null
        var node = new TermNode<string>(inputTerm);

        // Act
        var result = node.IsVoid();

        // Assert
        Assert.Equal(expectedIsVoid, result);
    }
}
