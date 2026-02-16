using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Model;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class QuerySyntaxHelperTests
{
	private readonly Mock<ITokenizer> _tokenizerMock;
	private readonly QuerySyntaxHelper _sut;

	public QuerySyntaxHelperTests()
	{
		_tokenizerMock = new Mock<ITokenizer>();
		_sut = new QuerySyntaxHelper(_tokenizerMock.Object);
	}

	[Theory]
	[InlineData("\"exact match\"", true)]
	[InlineData("\"a\"", true)]
	[InlineData("\"", false)] // Too short
	[InlineData("no quotes", false)]
	[InlineData("\"missing end", false)]
	public void IsPhraseToken_ShouldValidateCorrectQuotes(string token, bool expected)
	{
		Assert.Equal(expected, _sut.IsPhraseToken(token));
	}

	[Theory]
	[InlineData("AND", true)]
	[InlineData("&&", true)]
	[InlineData("OR", true)]
	[InlineData("||", true)]
	[InlineData("and", false)] 
	[InlineData("NOT", false)]
	public void TCFRQ3004_IsOperatorToken_ShouldIdentifySupportedOperators(string token, bool expected)
	{
		Assert.Equal(expected, _sut.IsOperatorToken(token));
	}

	[Theory]
	[InlineData("OR")]
	[InlineData("||")]
	public void TCFRQ3005_DetectMode_ShouldReturnOR_WhenOROperatorExists(string operand)
	{
		// Arrange & Act
		var tokens = new List<string> { "term1", operand, "term2" };

		// Assert
		Assert.Equal(QueryMode.OR, _sut.DetectMode(tokens));
	}

	[Fact]
	public void TCFRQ3005_DetectMode_ShouldDefaultToOR_WhenNoOperatorsExist()
	{
		// FRQ-3005: Whitespace/Default implies OR
		var tokens = new List<string> { "term1", "term2" };
		Assert.Equal(QueryMode.OR, _sut.DetectMode(tokens));
	}


	[Theory]
	[InlineData("AND")]
	[InlineData("&&")]
	public void TCFRQ3006_DetectMode_ShouldReturnAND_WhenANDOperatorExists(string operand)
	{
		// Arrange & Act
		var tokens = new List<string> { "term1", operand, "term2" };

		// Assert 
		Assert.Equal(QueryMode.AND, _sut.DetectMode(tokens));
	}
	
	[Fact]
	public void DetectMode_ShouldPrioritizeAND_IfBothExist()
	{
		// Current implementation checks AND first
		var tokens = new List<string> { "term1", "AND", "term2", "OR", "term3" };
		Assert.Equal(QueryMode.AND, _sut.DetectMode(tokens));
	}

	[Fact]
	public void Tokenize_ShouldCallInternalTokenizer()
	{
		var input = "search query";
		_tokenizerMock.Setup(x => x.Tokenize(input)).Returns(new List<string> { "search", "query" });

		var result = _sut.Tokenize(input);

		_tokenizerMock.Verify(x => x.Tokenize(input), Times.Once);
		Assert.Equal(2, result.Count);
	}
}
