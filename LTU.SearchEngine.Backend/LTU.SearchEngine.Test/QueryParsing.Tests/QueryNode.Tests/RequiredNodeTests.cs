using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests.QueryNode.Tests;

public class RequiredNodeTests
{
	[Fact]
	public void Constructor_ValidNode_SetsProperty()
	{
		// Arrange
		var innerNodeMock = new Mock<QueryNode<string>>().Object;

		// Act
		var requiredNode = new RequiredNode<string>(innerNodeMock);

		// Assert
		Assert.Same(innerNodeMock, requiredNode.Node);
	}

	[Fact]
	public void Constructor_NullNode_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(
			() => new RequiredNode<string>(null!)
		);
	}

	[Fact]
	public void Accept_CallsVisitOnVisitor_ReturnsExpectedValue()
	{
		// Arrange
		var innerNode = new Mock<QueryNode<Task<HashSet<int>>>>().Object;
		var sut = new RequiredNode<Task<HashSet<int>>>(innerNode);

		var mockVisitor = new Mock<IQueryVisitor<Task<HashSet<int>>>>();
		var expectedResult = Task.FromResult(
			new HashSet<int> { 42 }
		);

		mockVisitor
			.Setup(v => v.Visit(sut))
			.Returns(expectedResult);

		// Act
		var result = sut.Accept(mockVisitor.Object);

		// Assert
		Assert.Same(expectedResult, result);
		mockVisitor.Verify(v => v.Visit(sut), Times.Once);
	}
}
