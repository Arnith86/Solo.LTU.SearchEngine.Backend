using LTU.SearchEngine.Application.QueryParsing;
using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using LTU.SearchEngine.Test.HelperClasses;
using Moq;

namespace LTU.SearchEngine.Test.Application.Tests;

public class QueryParserTests
{
	private readonly Mock<ITreeBuilder<HashSet<int>, ExtractedQueryToken>> _treeBuilderMock;
	private readonly Mock<IStringTokenizer<ExtractedQueryToken, IgnoredTermsDTO>> _tokenizerMock;

	private readonly QueryParser _parser;

	public QueryParserTests()
	{
		_treeBuilderMock = 
			new Mock<ITreeBuilder<HashSet<int>, ExtractedQueryToken>>();
		_tokenizerMock = 
			new Mock<IStringTokenizer<ExtractedQueryToken, IgnoredTermsDTO>>();

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
		
		var queryStringTokenizingResult = 
			QueryStringTokenizingResultBuilder.BuildQueryStringTokenizingResult();

		_tokenizerMock
			.Setup(t => t.Tokenize(query))
			.Returns(queryStringTokenizingResult/*tokens*/);

		_treeBuilderMock
			.Setup(t => t.BuildTree(queryStringTokenizingResult.Tokens))
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
	
		var queryStringTokenizingResult = 
			QueryStringTokenizingResultBuilder.BuildQueryStringTokenizingResult();

		_tokenizerMock
			.Setup(t => t.Tokenize(query))
			.Returns(queryStringTokenizingResult);

		_treeBuilderMock
			.Setup(t => t.BuildTree(queryStringTokenizingResult.Tokens))
			.Returns(Mock.Of<QueryNode<HashSet<int>>>());

		// Act
		_parser.Parse(query);

		// Assert
		_treeBuilderMock.Verify(t => t.BuildTree(queryStringTokenizingResult.Tokens), Times.Once);
	}

	[Fact]
	public void Parse_ShouldReturnRootNodeFromTreeBuilder()
	{
		// Arrange
		string query = "cat";
		
		var queryStringTokenizingResult = 
			QueryStringTokenizingResultBuilder.BuildQueryStringTokenizingResult();

		var expectedResult = QueryParsingResultBuilder.BuildQueryParsingResult();
	
		_tokenizerMock
			.Setup(t => t.Tokenize(query))
			.Returns(queryStringTokenizingResult);

		_treeBuilderMock
			.Setup(t => t.BuildTree(queryStringTokenizingResult.Tokens))
			.Returns(expectedResult.RootNode);

		// Act
		var result = _parser.Parse(query);

		// Assert
		Assert.Equivalent(expectedResult, result);
	}
	
	
    [Theory]
    [InlineData("sv", "sv")]
    [InlineData("en", "en")]
    [InlineData("NO_INPUT", "sv")]
	public void Parse_ShouldUseCorrectLanguageCode(string input, string expected)
	{
		// Arrange
		string query = "cat";
		// var tokens = new List<ExtractedQueryToken>();
		var queryStringTokenizingResult = 
				QueryStringTokenizingResultBuilder.BuildQueryStringTokenizingResult();

		var expectedResult = QueryParsingResultBuilder.BuildQueryParsingResult();
		// var expectedNode = Mock.Of<QueryNode<HashSet<int>>>();

		_tokenizerMock
			.Setup(t => t.Tokenize(query, languageCode: expected))
			.Returns(queryStringTokenizingResult);

		_treeBuilderMock
			.Setup(t => t.BuildTree(queryStringTokenizingResult.Tokens))
			.Returns(expectedResult.RootNode);

		Func</*QueryNode<HashSet<int>>*/QueryParsingResult<HashSet<int>, IgnoredTermsDTO>> act = input switch
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
