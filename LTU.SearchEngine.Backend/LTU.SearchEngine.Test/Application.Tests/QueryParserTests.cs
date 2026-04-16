using LTU.SearchEngine.Application.QueryParsing;
using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using Moq;

namespace LTU.SearchEngine.Test.Application.Tests;

public class QueryParserTests
{
	private readonly Mock<ITreeBuilder<HashSet<int>, ExtractedQueryToken>> _treeBuilderMock;
	private readonly Mock<IStringTokenizer<ExtractedQueryToken, QueryTokenType>> _tokenizerMock;

	private readonly QueryParser _parser;

	public QueryParserTests()
	{
		_treeBuilderMock = 
			new Mock<ITreeBuilder<HashSet<int>, ExtractedQueryToken>>();
		_tokenizerMock = 
			new Mock<IStringTokenizer<ExtractedQueryToken, QueryTokenType>>();

		_parser = new QueryParser(_treeBuilderMock.Object, _tokenizerMock.Object);
	}

	[Fact]
	public void Constructor_NullTreeBuilder_ShouldThrowArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new QueryParser(null!, _tokenizerMock.Object));
	}

	[Fact]
	public void Constructor_NullTokenizer_ShouldThrowArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new QueryParser(_treeBuilderMock.Object, null!));
	}

	[Fact]
	public void Parse_ShouldTokenizeQuery()
	{
		// Arrange
		string query = "cat AND dog";
		var tokens = new List<ExtractedQueryToken>();

		_tokenizerMock
			.Setup(t => t.Tokenize(query))
			.Returns(tokens);

		_treeBuilderMock
			.Setup(t => t.BuildTree(tokens))
			.Returns(Mock.Of<QueryNode<HashSet<int>>>());

		// Act
		_parser.Parse(query);

		// Assert
		_tokenizerMock.Verify(t => t.Tokenize(query), Times.Once);
	}

	[Fact]
	public void Parse_ShouldPassTokensToTreeBuilder()
	{
		// Arrange
		string query = "cat";
		var tokens = new List<ExtractedQueryToken>();

		_tokenizerMock
			.Setup(t => t.Tokenize(query))
			.Returns(tokens);

		_treeBuilderMock
			.Setup(t => t.BuildTree(tokens))
			.Returns(Mock.Of<QueryNode<HashSet<int>>>());

		// Act
		_parser.Parse(query);

		// Assert
		_treeBuilderMock.Verify(t => t.BuildTree(tokens), Times.Once);
	}

	[Fact]
	public void Parse_ShouldReturnRootNodeFromTreeBuilder()
	{
		// Arrange
		string query = "cat";
		var tokens = new List<ExtractedQueryToken>();

		var expectedNode = Mock.Of<QueryNode<HashSet<int>>>();

		_tokenizerMock
			.Setup(t => t.Tokenize(query))
			.Returns(tokens);

		_treeBuilderMock
			.Setup(t => t.BuildTree(tokens))
			.Returns(expectedNode);

		// Act
		var result = _parser.Parse(query);

		// Assert
		Assert.Equal(expectedNode, result);
	}
	
	
    [Theory]
    [InlineData("sv", "sv")]
    [InlineData("en", "en")]
    [InlineData("NO_INPUT", "sv")]
	public void Parse_ShouldUseCorrectLanguageCode(string input, string expected)
	{
		// Arrange
		string query = "cat";
		var tokens = new List<ExtractedQueryToken>();

		var expectedNode = Mock.Of<QueryNode<HashSet<int>>>();

		_tokenizerMock
			.Setup(t => t.Tokenize(query, languageCode: expected))
			.Returns(tokens);

		_treeBuilderMock
			.Setup(t => t.BuildTree(tokens))
			.Returns(expectedNode);

		Func<QueryNode<HashSet<int>>> act = input switch
        {
            "NO_INPUT"  => () => _parser.Parse(query),
            _           => () => _parser.Parse(query, languageCode: input)
        };

		// Act
		act();

		// Assert
		_tokenizerMock.Verify(t => t.Tokenize(
			input: It.IsAny<string>(),
			languageCode: expected
		));
	}
}
