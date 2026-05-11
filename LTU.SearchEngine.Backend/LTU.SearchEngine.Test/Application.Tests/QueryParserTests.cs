using LTU.SearchEngine.Application.QueryParsing;
using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.RequestParameters;
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
		var searchParam = SearchQueryRequestParametersBuilder.BuildParameters(query);
        
		var queryStringTokenizingResult = 
			QueryStringTokenizingResultBuilder.BuildQueryStringTokenizingResult();

		_tokenizerMock
			.Setup(t => t.Tokenize(searchParam))
			.Returns(queryStringTokenizingResult);

		_treeBuilderMock
			.Setup(t => t.BuildTree(queryStringTokenizingResult.Tokens))
			.Returns(Mock.Of<QueryNode<HashSet<int>>>());

		// Act
		_parser.Parse(searchParam);

		// Assert
		_tokenizerMock.Verify(t => t.Tokenize(searchParam), Times.Once);
	}

	[Fact]
	public void Parse_ShouldPassTokensToTreeBuilder()
	{
		// Arrange
		string query = "cat";
		var searchParam = SearchQueryRequestParametersBuilder.BuildParameters(query);

		var queryStringTokenizingResult = 
			QueryStringTokenizingResultBuilder.BuildQueryStringTokenizingResult();

		_tokenizerMock
			.Setup(t => t.Tokenize(searchParam))
			.Returns(queryStringTokenizingResult);

		_treeBuilderMock
			.Setup(t => t.BuildTree(queryStringTokenizingResult.Tokens))
			.Returns(Mock.Of<QueryNode<HashSet<int>>>());

		// Act
		_parser.Parse(searchParam);

		// Assert
		_treeBuilderMock.Verify(t => t.BuildTree(queryStringTokenizingResult.Tokens), Times.Once);
	}

	[Fact]
	public void Parse_ShouldReturnRootNodeFromTreeBuilder()
	{
		// Arrange
		string query = "cat";
		var searchParam = SearchQueryRequestParametersBuilder.BuildParameters(query);

		var queryStringTokenizingResult = 
			QueryStringTokenizingResultBuilder.BuildQueryStringTokenizingResult();

		var expectedResult = QueryParsingResultBuilder.BuildQueryParsingResult();
	
		_tokenizerMock
			.Setup(t => t.Tokenize(searchParam))
			.Returns(queryStringTokenizingResult);

		_treeBuilderMock
			.Setup(t => t.BuildTree(queryStringTokenizingResult.Tokens))
			.Returns(expectedResult.RootNode);

		// Act
		var result = _parser.Parse(searchParam);

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
		
		var tokenizerResult = QueryStringTokenizingResultBuilder.BuildQueryStringTokenizingResult();
		var expectedResult = QueryParsingResultBuilder.BuildQueryParsingResult();
	
		_tokenizerMock
        .Setup(t => t.Tokenize(It.IsAny<SearchQueryRequestParameters>()))
        .Returns(tokenizerResult);

		_treeBuilderMock
			.Setup(t => t.BuildTree(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(expectedResult.RootNode);

		var searchParam = input == "NO_INPUT" 
			? SearchQueryRequestParametersBuilder.BuildParameters(query)
			: SearchQueryRequestParametersBuilder.BuildParameters(query, language: input);
	
		// Act
		_parser.Parse(searchParam);

		// Assert
		_tokenizerMock.Verify(t => t.Tokenize(
			It.Is<SearchQueryRequestParameters>(sqrp => sqrp.Language == expected)
		), Times.Once);
	}

	[Fact]
	public void Parse_ShouldIncludeIgnoredTokensInResult()
	{
		// Arrange
		var query = "test";
		var ignored = new List<IgnoredTermsDTO> { new() { Token = "stopword", Language = "sv" } };
		
		var tokenizerResult = new QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO>(
			new List<ExtractedQueryToken>(), 
			ignored
		);

		var searchParam = SearchQueryRequestParametersBuilder.BuildParameters(query, It.IsAny<string>());
		
		_tokenizerMock.Setup(t => t.Tokenize(searchParam))
			.Returns(tokenizerResult);

		_treeBuilderMock.Setup(t => t.BuildTree(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
			.Returns(Mock.Of<QueryNode<HashSet<int>>>());

		// Act
		var result = _parser.Parse(searchParam);

		// Assert
		Assert.Equal(ignored, result.IgnoredTokens);
	}

	// [Fact]
	// public void Parse_WhenNoTokens_ShouldStillReturnResultWithRootNode()
	// {
	// 	// Arrange
	// 	var tokenizerResult = new QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO>(
	// 		Enumerable.Empty<ExtractedQueryToken>(),
	// 		Enumerable.Empty<IgnoredTermsDTO>()
	// 	);

	// 	// var searchParam = SearchQueryRequestParametersBuilder.BuildParameters(" ", "sv");
	// 	var searchParam = new SearchQueryRequestParameters { Query = " ", Language = "sv"};
		
	// 	_tokenizerMock.Setup(t => t.Tokenize(searchParam)).Returns(tokenizerResult);
	// 	_treeBuilderMock
	// 		.Setup(t => t.BuildTree(It.IsAny<IEnumerable<ExtractedQueryToken>>()))
	// 		.Returns(Mock.Of<QueryNode<HashSet<int>>>());

	// 	// Act
	// 	var result = _parser.Parse(searchParam);

	// 	// Assert
	// 	Assert.NotNull(result.RootNode);
	// 	Assert.Empty(result.IgnoredTokens);
	// }
}
