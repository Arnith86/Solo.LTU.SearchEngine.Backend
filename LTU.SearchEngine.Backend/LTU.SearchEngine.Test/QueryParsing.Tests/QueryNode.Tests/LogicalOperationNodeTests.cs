using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests.QueryNode.Tests;

public class LogicalOperationNodeTests
{
	[Theory]
	[InlineData(LogicalOperators.AND)]
	[InlineData(LogicalOperators.OR)]
	[InlineData(LogicalOperators.NOT)]
	public void Constructor_ValidNodes_SetsProperties(LogicalOperators logicalOperators)
	{
		// Arrange
		string left = "left";
		string right = "right";
		
		var leftNode = new TermNode<string>(left);
		var rightNode = new TermNode<string>(right);

		//Act
		var sut = new LogicOperationNode<string>(
			leftNode,
			rightNode,
			logicalOperators
		);


		// Assert
		Assert.Equivalent(leftNode, sut.LeftNode);
		Assert.Equivalent(rightNode, sut.RightNode);
		Assert.Equal(logicalOperators, sut.LogicalOperator);
	}

	[Fact]
	public void Constructor_NullArguments_ThrowsArgumentNullException()
	{
		// Arrange
		var dummy = new TermNode<string>("Dummy");
		
		// Act & Assert
		Assert.Throws<ArgumentNullException>(
			() => new LogicOperationNode<string>(null!, dummy, LogicalOperators.AND)
		);
		Assert.Throws<ArgumentNullException>(
			() => new LogicOperationNode<string>(dummy, null!, LogicalOperators.AND)
		);
	}

	[Fact]
	public void Constructor_InvalidLogicOperator_ThrowsArgumentException()
	{
		// Arrange
		var dummy = new TermNode<string>("Dummy");

		// Act & Assert
		Assert.Throws<ArgumentException>(
			() => new LogicOperationNode<string>(dummy, dummy, LogicalOperators.REQUIRED)
		);
	}

	[Fact]
	public void Accept_CallsVisitOnVisitor_ReturnsExpectedValue()
	{
		// Arrange
		var dummy = new TermNode<Task<HashSet<int>>>("dummy");
		
		var sut = new LogicOperationNode<Task<HashSet<int>>>(
			dummy,
			dummy,
			LogicalOperators.AND
		);

		var mockVisitor = new Mock<IQueryVisitor<Task<HashSet<int>>>>();

		var expectedResult = Task.FromResult(new HashSet<int> { 1, 2, 3 });

		mockVisitor
			.Setup(v => v.Visit(sut))
			.Returns(expectedResult);

		// Act
		var result = sut.Accept(mockVisitor.Object);

		// Assert
		Assert.Equal(expectedResult, result);
		mockVisitor.Verify(v => v.Visit(sut), Times.Once);
	}
}
