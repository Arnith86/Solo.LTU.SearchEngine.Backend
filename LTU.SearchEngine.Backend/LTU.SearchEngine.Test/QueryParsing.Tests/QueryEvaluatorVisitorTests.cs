using LTU.SearchEngine.Api.ExtensionsUseExceptionHandler.CustomExceptions;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Infrastructure.Repositories;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class QueryEvaluatorVisitorTests
{
	private readonly Mock<IIndexRepository> _repositoryMock;
	private readonly QueryEvaluatorVisitor _sut;

	public QueryEvaluatorVisitorTests()
	{
		_repositoryMock = new Mock<IIndexRepository>();
		_sut = new QueryEvaluatorVisitor(_repositoryMock.Object);
	}

	[Fact]
	public void Constructor_ValidRepository_ShouldCreateInstance()
	{
		// Act 
		var sut = new QueryEvaluatorVisitor(_repositoryMock.Object);

		// Assert
		Assert.NotNull(sut);
		Assert.IsType<QueryEvaluatorVisitor>(sut);
	}

	[Fact]
	public async Task Constructor_RepositoryNull_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new QueryEvaluatorVisitor(null!));
	}

	[Fact]
	public async Task ExecuteAsync_TermNode_ReturnsRepositoryResult()
	{
		// Arrange
		var expectedIds = new HashSet<int> { 1, 2, 3 };
		
		_repositoryMock
			.Setup(r => r.GetDocumentIdsForTermAsync("test"))
			.ReturnsAsync(expectedIds);

		var node = new TermNode<HashSet<int>>("test");

		// Act
		var result = await _sut.ExecuteAsync(node);

		// Assert
		Assert.Equal(expectedIds, result);
	}

	[Fact]
	public async Task ExecuteAsync_AND_ReturnsRepositoryResult()
	{
		// Arrange
		var leftSet = new HashSet<int> { 1, 2, 3 };
		var rightSet = new HashSet<int> { 2, 3, 4 };
		var expectedIds = new HashSet<int> { 2, 3 };

		_repositoryMock
			.Setup(r => r.GetDocumentIdsForTermAsync("left"))
			.ReturnsAsync(leftSet);

		_repositoryMock
			.Setup(r => r.GetDocumentIdsForTermAsync("right"))
			.ReturnsAsync(rightSet);



		var node = new LogicOperationNode<HashSet<int>>(
			new TermNode<HashSet<int>>("left"),
			new TermNode<HashSet<int>>("right"),
			LogicalOperators.AND
		);

		// Act
		var result = await _sut.ExecuteAsync(node);

		// Assert
		Assert.Equal(expectedIds, result);
	}
	
	[Fact]
	public async Task ExecuteAsync_OR_ReturnsRepositoryResult()
	{
		// Arrange
		var leftSet = new HashSet<int> { 1, 2, 3 };
		var rightSet = new HashSet<int> { 2, 3, 4 };
		var expectedIds = new HashSet<int> { 1, 2, 3, 4 };

		_repositoryMock
			.Setup(r => r.GetDocumentIdsForTermAsync("left"))
			.ReturnsAsync(leftSet);

		_repositoryMock
			.Setup(r => r.GetDocumentIdsForTermAsync("right"))
			.ReturnsAsync(rightSet);



		var node = new LogicOperationNode<HashSet<int>>(
			new TermNode<HashSet<int>>("left"),
			new TermNode<HashSet<int>>("right"),
			LogicalOperators.OR
		);

		// Act
		var result = await _sut.ExecuteAsync(node);

		// Assert
		Assert.Equal(expectedIds, result);
	}

	[Fact]
	public async Task ExecuteAsync_NestedQuery_ReturnsRepositoryResult()
	{
		// (one AND (two OR three))
		// Arrange
		var one = new HashSet<int> { 1, 2, 3 };
		var two = new HashSet<int> { 2 };
		var three = new HashSet<int> { 3 };
		var expectedIds = new HashSet<int> { 2, 3 };

		_repositoryMock
			.Setup(r => r.GetDocumentIdsForTermAsync("one"))
			.ReturnsAsync(one);

		_repositoryMock
			.Setup(r => r.GetDocumentIdsForTermAsync("two"))
			.ReturnsAsync(two);
		
		_repositoryMock
			.Setup(r => r.GetDocumentIdsForTermAsync("three"))
			.ReturnsAsync(three);

		var innerNode = new LogicOperationNode<HashSet<int>>(
			new TermNode<HashSet<int>>("two"),
			new TermNode<HashSet<int>>("three"),
			LogicalOperators.OR
		);

		var outerNode = new LogicOperationNode<HashSet<int>>(
			new TermNode<HashSet<int>>("one"),
			innerNode,
			LogicalOperators.AND
		);

		// Act
		var result = await _sut.ExecuteAsync(outerNode);

		// Assert
		Assert.Equal(expectedIds, result);
	}

	[Fact]
	public async Task ExecuteAsync_PhraseNode_ReturnsRepositoryResult()
	{
		// Arrange
		var phrase = new PhraseNode<HashSet<int>>(
			new List<ExtractedQueryToken>()
			{
				new ExtractedQueryToken(QueryTokenType.Term, "word1"),
				new ExtractedQueryToken(QueryTokenType.Term, "word2")
			}
		);

		var expected = new HashSet<int> { 5, 6 };


		_repositoryMock
			.Setup(r => r.GetDocumentIdsForPhraseAsync(phrase))
			.ReturnsAsync(expected);

		// Act 
		var result = await _sut.ExecuteAsync(phrase);

		// Assert
		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task VisitAsync_LogicNode_NOT_WithVoidLeft_ShouldReturnEmptySet()
	{
		// Arrange: "" NOT "dog" -> Void
		var leftNode = new TermNode<HashSet<int>>(string.Empty); // Assumed to implement IIsVoidable and return true
		var rightNode = new TermNode<HashSet<int>>("dog");
		
		_repositoryMock.Setup(r => r.GetDocumentIdsForTermAsync("dog"))
					.ReturnsAsync(new HashSet<int> { 1 });

		var node = new LogicOperationNode<HashSet<int>>(leftNode, rightNode, LogicalOperators.NOT);

		// Act
		var result = await _sut.VisitAsync(node);

		// Assert
		Assert.Empty(result);
		_repositoryMock.Verify(r => r.GetDocumentIdsForTermAsync("dog"), Times.Never);
	}

	[Fact]
	public async Task VisitAsync_LogicNode_WithOneVoidChild_ShouldReturnOtherChildResult()
	{
		// Arrange: "cat" OR "" -> Should just evaluate "cat"
		var expectedIds = new HashSet<int> { 1, 2 };
		var leftNode = new TermNode<HashSet<int>>("cat");
		var rightNode = new TermNode<HashSet<int>>(string.Empty); // Void node

		_repositoryMock.Setup(r => r.GetDocumentIdsForTermAsync("cat"))
					.ReturnsAsync(expectedIds);

		var node = new LogicOperationNode<HashSet<int>>(leftNode, rightNode, LogicalOperators.OR);

		// Act
		var result = await _sut.VisitAsync(node);

		// Assert
		Assert.Equal(expectedIds, result);
		_repositoryMock.Verify(r => r.GetDocumentIdsForTermAsync("cat"), Times.Once);
	}

	[Fact]
	public async Task VisitAsync_LogicNode_BothChildrenVoid_ShouldReturnEmptySet()
	{
		// Arrange: "" AND ""
		var leftNode = new TermNode<HashSet<int>>(string.Empty);
		var rightNode = new TermNode<HashSet<int>>(string.Empty);
		var node = new LogicOperationNode<HashSet<int>>(leftNode, rightNode, LogicalOperators.AND);

		// Act
		var result = await _sut.VisitAsync(node);

		// Assert
		Assert.Empty(result);
		_repositoryMock.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task VisitAsync_TermNode_WhenRepositoryThrows_ShouldThrowQueryEvaluationException()
	{
		// Arrange
		_repositoryMock.Setup(r => r.GetDocumentIdsForTermAsync(It.IsAny<string>()))
					.ThrowsAsync(new Exception("Database down"));
		var node = new TermNode<HashSet<int>>("fail");

		// Act & Assert
		var ex = await Assert.ThrowsAsync<QueryEvaluationException>(() => _sut.VisitAsync(node));
		Assert.Contains("Failed evaluating term 'fail'", ex.Message);
	}
}
