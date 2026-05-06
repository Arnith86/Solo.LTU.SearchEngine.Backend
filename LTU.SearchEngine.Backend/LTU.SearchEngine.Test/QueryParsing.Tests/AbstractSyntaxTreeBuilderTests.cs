using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Exceptions.SearchQueryExceptions;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class AbstractSyntaxTreeBuilderTests
{
	private readonly Mock<IShuntingYardParser<ExtractedQueryToken>> _parserMock;

	public AbstractSyntaxTreeBuilderTests()
	{
		_parserMock = new Mock<IShuntingYardParser<ExtractedQueryToken>>();
	}

	private AbstractSyntaxTreeBuilder<HashSet<int>> CreateBuilder()
		=> new AbstractSyntaxTreeBuilder<HashSet<int>>(_parserMock.Object);

	[Fact]
	public void BuildTree_SingleTerm_ReturnsTermNode()
	{
		// Arrange
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Term, "cat")
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var sut = CreateBuilder();

		// Act
		var result = sut.BuildTree(new List<ExtractedQueryToken>());

		// Assert
		var termNode = 
			Assert.IsType<TermNode<HashSet<int>>>(result);
		Assert.Equal("cat", termNode.Term);
	}

	[Fact]
	public void BuildTree_ANDOperator_CreatesLogicOperationNode()
	{
		// Arrange
		// postfix: cat dog AND
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Term, "cat"),
			new ExtractedQueryToken(QueryTokenType.Term, "dog"),
			new ExtractedQueryToken(QueryTokenType.LogicalOperator, "AND")
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var sut = CreateBuilder();
		
		// Act
		var result = sut.BuildTree(new List<ExtractedQueryToken>());

		// Assert 
		var logicNode = 
			Assert.IsType<LogicOperationNode<HashSet<int>>>(result);

		Assert.Equal(LogicalOperators.AND, logicNode.LogicalOperator);
		Assert.IsType<TermNode<HashSet<int>>>(logicNode.LeftNode);
		Assert.IsType<TermNode<HashSet<int>>>(logicNode.RightNode);
	}

	[Theory]
	[InlineData("NOT", "OR", "AND", "(mouse AND (cat OR (dog NOT fish)))")]
	[InlineData("AND", "AND", "AND", "(mouse AND (cat AND (dog AND fish)))")]
	[InlineData("OR", "OR", "OR", "(mouse OR (cat OR (dog OR fish)))")]
	[InlineData("NOT", "NOT", "OR", "(mouse OR (cat NOT (dog NOT fish)))")]
	[InlineData("NOT", "NOT", "AND", "(mouse AND (cat NOT (dog NOT fish)))")]
	[InlineData("AND", "OR", "OR", "(mouse OR (cat OR (dog AND fish)))")]
	public void BuildTree_NestedLogic_CorrectStructure(
		string innerOp, string middleOp, string outerOp, string expected)
	{
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
		new ExtractedQueryToken(QueryTokenType.Term, "mouse"),
		new ExtractedQueryToken(QueryTokenType.Term, "cat"),
		new ExtractedQueryToken(QueryTokenType.Term, "dog"),
		new ExtractedQueryToken(QueryTokenType.Term, "fish"),
        new ExtractedQueryToken(QueryTokenType.LogicalOperator, innerOp),
		new ExtractedQueryToken(QueryTokenType.LogicalOperator, middleOp),
		new ExtractedQueryToken(QueryTokenType.LogicalOperator, outerOp)
	});

		_parserMock.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
				   .Returns(postfix);

		var builder = CreateBuilder();

		var result = builder.BuildTree(new List<ExtractedQueryToken>());

		Assert.Equal(
			expected,
			result.ToString());
	}

	[Fact]
	public void BuildTree_PhraseToken_CreatesPhraseNode()
	{
		// Arrange
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Phrase, "hello world")
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var sut = CreateBuilder();

		// Act
		var result = sut.BuildTree(new List<ExtractedQueryToken>());

		// Assert
		var phraseNode = Assert.IsType<PhraseNode<HashSet<int>>>(result);
		Assert.Equal(2, phraseNode.Phrase.Count);
		Assert.Equal("hello", phraseNode.Phrase[0].Token);
		Assert.Equal("world", phraseNode.Phrase[1].Token);
	}


	[Fact]
	public void BuildTree_RequiredTerm_CreatesNodeWithIsRequiredTrue()
	{
		// Arrange
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Term, "cat", RequirementLevel.Required)
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var sut = CreateBuilder();

		// Act
		var result = sut.BuildTree(new List<ExtractedQueryToken>());

		// Assert
		var termNode = Assert.IsType<TermNode<HashSet<int>>>(result);
		Assert.True(termNode.IsRequired()); 
	}


	[Fact]
	public void BuildTree_RequiredPhrase_CreatesNodeWithIsRequiredTrue()
	{
		// Arrange
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Phrase, "luleå ltu", RequirementLevel.Required)
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var sut = CreateBuilder();

		// Act
		var result = sut.BuildTree(new List<ExtractedQueryToken>());

		// Assert
		var phraseNode = Assert.IsType<PhraseNode<HashSet<int>>>(result);
		Assert.True(phraseNode.IsRequired());
	}


	[Fact]
	public void BuildTree_RequiredLogicalOperator_CreatesLogicNodeWithIsRequiredTrue()
	{
		// Arrange: Postfix for "cat dog AND" where AND is tagged as Required
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Term, "cat"),
			new ExtractedQueryToken(QueryTokenType.Term, "dog"),
			new ExtractedQueryToken(QueryTokenType.LogicalOperator, "AND", RequirementLevel.Required)
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var sut = CreateBuilder();

		// Act
		var result = sut.BuildTree(new List<ExtractedQueryToken>());

		// Assert
		var logicNode = Assert.IsType<LogicOperationNode<HashSet<int>>>(result);
		Assert.True(logicNode.IsRequired());
		
		// Child nodes should still be optional unless they were specifically tagged
		var left = Assert.IsType<TermNode<HashSet<int>>>(logicNode.LeftNode);
		Assert.False(left!.IsRequired());
		
		var right = Assert.IsType<TermNode<HashSet<int>>>(logicNode.RightNode);
		Assert.False(right!.IsRequired());
	}


	[Fact]
	public void BuildTree_MixedRequirements_AssignsPropertiesCorrectly()
	{
		// Arrange: +cat dog OR (Optional OR, but 'cat' is required)
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Term, "cat", RequirementLevel.Required),
			new ExtractedQueryToken(QueryTokenType.Term, "dog", RequirementLevel.Optional),
			new ExtractedQueryToken(QueryTokenType.LogicalOperator, "OR", RequirementLevel.Optional)
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var sut = CreateBuilder();

		// Act
		var result = sut.BuildTree(new List<ExtractedQueryToken>());

		// Assert
		var logicNode = Assert.IsType<LogicOperationNode<HashSet<int>>>(result);
		Assert.False(logicNode.IsRequired()); // The OR itself is optional
		
		var left = Assert.IsType<TermNode<HashSet<int>>>(logicNode.LeftNode);
		Assert.True(left.IsRequired()); // cat is required
		
		var right = Assert.IsType<TermNode<HashSet<int>>>(logicNode.RightNode);
		Assert.False(right.IsRequired()); // dog is optional
	}


	[Fact]
	public void BuildTree_InvalidStructure_Throws()
	{
		// Arrange
		// Two terms, no operator → stack.Count != 1
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Term, "cat"),
			new ExtractedQueryToken(QueryTokenType.Term, "dog")
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var builder = CreateBuilder();

		// Act & Assert
		Assert.Throws<QuerySyntaxException>(() =>
			builder.BuildTree(new List<ExtractedQueryToken>())
		);
	}

	[Fact]
	public void BuildTree_InvalidLogicalOperation_Throws()
	{
		// Arrange
		// Only one operand before AND
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Term, "cat"),
			new ExtractedQueryToken(QueryTokenType.LogicalOperator, "AND")
		});

		_parserMock.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
				   .Returns(postfix);

		var builder = CreateBuilder();


		// Act & Assert
		Assert.Throws<QuerySyntaxException>(() =>
			builder.BuildTree(new List<ExtractedQueryToken>())
		);
	}

	[Fact]
	public void BuildTree_UnsupportedOperator_Throws()
	{
		// Arrange
		var postfix = new Queue<ExtractedQueryToken>(new[]
		{
			new ExtractedQueryToken(QueryTokenType.Term, "cat"),
			new ExtractedQueryToken(QueryTokenType.Term, "dog"),
			new ExtractedQueryToken(QueryTokenType.LogicalOperator, "XOR")
		});

		_parserMock
			.Setup(p => p.ConvertToPostfix(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(postfix);

		var builder = CreateBuilder();

		// Act & Assert
		Assert.Throws<NotSupportedException>(() =>
			builder.BuildTree(new List<ExtractedQueryToken>())
		);
	}
}
