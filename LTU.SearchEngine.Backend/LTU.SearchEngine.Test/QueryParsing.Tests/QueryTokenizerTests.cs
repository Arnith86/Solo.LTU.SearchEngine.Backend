using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Moq;
using System;
using System.Text;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class QueryTokenizerTests
{
	private readonly QueryStringTokenizer _sut;
	private readonly Mock<IQuerySyntaxHelper> _mockSyntaxHelper;

	public QueryTokenizerTests()
	{
		_mockSyntaxHelper = new Mock<IQuerySyntaxHelper>();
		_sut = new QueryStringTokenizer(_mockSyntaxHelper.Object);
	}

	[Fact]
	public void Constructor_NullIQuerySyntaxHelper_ShouldThrowArgumentNullException()
	{
		// Act & Assert 
		Assert.Throws<ArgumentNullException>(() => new QueryStringTokenizer(null!));
	}

	[Fact]
	public void Tokenize_ValidateGroupThrowsInvalidQueryStringException_ShouldPropagate()
	{
		// Arrange
		var input = "(()";

		var first = new ExtractedQueryToken(QueryTokenType.Term, "(");
		var second = new ExtractedQueryToken(QueryTokenType.Term, "(");
		var third = new ExtractedQueryToken(QueryTokenType.Term, ")");

		var expected = new List<ExtractedQueryToken> { first, second, third };

		_mockSyntaxHelper
			.Setup(sh => sh.ValidateGrouping(It.IsAny<List<ExtractedQueryToken>>()))
			.Throws(new InvalidQueryStringException("Mismatched parentheses", input));

		// Act & Assert 
		Assert.Throws<InvalidQueryStringException>(() => _sut.Tokenize(input));
	}

	[Fact]
	public void Tokenize_SimpleWords_ReturnsSeparateTokens()
	{
		// Arrange
		var input = "apple orange banana";
		
		var apple = new ExtractedQueryToken(QueryTokenType.Term, "apple");
		var orange = new ExtractedQueryToken(QueryTokenType.Term, "orange");
		var banana = new ExtractedQueryToken(QueryTokenType.Term, "banana");

		var expected = new List<ExtractedQueryToken> { apple, orange, banana };

		// Act 
		var result = _sut.Tokenize(input);

		// Assert 
		Assert.Equivalent(expected, result);
	}

	[Fact]
	public void Tokenize_QuotedPhrase_ReturnsPhraseAsSingleToken()
	{
		// Arrange
		var input = "cat \"hello dolly\" dog";

		var cat = new ExtractedQueryToken(QueryTokenType.Term, "cat");
		var helloDolly = new ExtractedQueryToken(QueryTokenType.Phrase, "hello dolly");
		var dog = new ExtractedQueryToken(QueryTokenType.Term, "dog");

		// Act 
		var result = _sut.Tokenize(input);

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equivalent(cat, result[0]);
		Assert.Equivalent(helloDolly, result[1]);
		Assert.Equivalent(dog, result[2]);
	}

	[Fact]
	public void Tokenize_ExtraWhitespace_ShouldBeIgnored()
	{
		// Arrange
		var input = "  word1    word2  ";
		var word1 = new ExtractedQueryToken(QueryTokenType.Term, "word1");
		var word2 = new ExtractedQueryToken(QueryTokenType.Term, "word2");

		// Act
		var result = _sut.Tokenize(input);

		// Assert
		Assert.Equivalent(new[] { word1, word2}, result);
	}

	[Fact]
	public void Tokenize_EmptyInput_ReturnsEmptyList()
	{
		// Act & Assert 
		Assert.Empty(_sut.Tokenize(""));
		Assert.Empty(_sut.Tokenize("   "));
	}

	[Fact]
	public void Tokenize_UnclosedQuotes_TreatsAllWordsAsTerms()
	{
		// Arrange
		var input = "start \"unclosed phrase";

		var start = new ExtractedQueryToken(QueryTokenType.Term, "start");
		var unclosed = new ExtractedQueryToken(QueryTokenType.Term, "\"unclosed");
		var phrase = new ExtractedQueryToken(QueryTokenType.Term, "phrase");

		// Act
		var result = _sut.Tokenize(input);

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equivalent(start, result[0]);
		Assert.Equivalent(unclosed, result[1]);
		Assert.Equivalent(phrase, result[2]);
	}


	[Theory]
	[InlineData("!")]
	[InlineData("-")]
	[InlineData("+")]
	[InlineData("&&")]
	[InlineData("||")]
	[InlineData("AND")]
	[InlineData("OR")]
	[InlineData("NOT")]
	public void Tokenize_Operators_HandledAsLogicalOperators(string operatorInput)
	{
		// Arrange
		var input = $"start {operatorInput} phrase";

		var start = new ExtractedQueryToken(QueryTokenType.Term, "start");
		var expectedOperator = new ExtractedQueryToken(QueryTokenType.LogicalOperator, operatorInput);
		var phrase = new ExtractedQueryToken(QueryTokenType.Term, "phrase");

		// Act
		var result = _sut.Tokenize(input);

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equivalent(start, result[0]);
		Assert.Equivalent(expectedOperator, result[1]);
		Assert.Equivalent(phrase, result[2]);
		Assert.Equal(expectedOperator.TokenType, result[1].TokenType);
	}

	[Theory]
	[InlineData("(", ")")]
	[InlineData("{", "}")]
	[InlineData("[", "]")]
	public void Tokenize_Operators_HandledAsGroupingOperators(string operator1, string operator2)
	{
		// Arrange
		var input = $"{operator1}\"start phrase\"{operator2}";

		var expectedOperator1 = new ExtractedQueryToken(QueryTokenType.GroupingOperator, operator1);
		var expectedPhrase = new ExtractedQueryToken(QueryTokenType.Phrase, "start phrase");
		var expectedOperator2 = new ExtractedQueryToken(QueryTokenType.GroupingOperator, operator2);

		// Act
		var result = _sut.Tokenize(input);

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equivalent(expectedOperator1, result[0]);
		Assert.Equivalent(expectedPhrase, result[1]);
		Assert.Equivalent(expectedOperator2, result[2]);
		Assert.Equal(expectedOperator1.TokenType, result[0].TokenType);
		Assert.Equal(expectedOperator2.TokenType, result[2].TokenType);
	}


	[Theory]
	[InlineData(QueryTokenType.Term)]
	[InlineData(QueryTokenType.Phrase)]
	[InlineData(QueryTokenType.LogicalOperator)]
	public void Flush_EmptyBuilder_DoesNotAddToken(QueryTokenType type)
	{
		// Arrange
		var tokens = new List<ExtractedQueryToken>();
		var sb = new StringBuilder();

		// Act
		_sut.Flush(sb, tokens, queryTokenType: type);

		// Assert
		Assert.Empty(tokens);
	}

	[Theory]
	[InlineData("   term   ",  QueryTokenType.Term)]
	[InlineData("  \" phrase \"  ", QueryTokenType.Phrase)]
	[InlineData("  !  ", QueryTokenType.LogicalOperator)]
	[InlineData("  -  ", QueryTokenType.LogicalOperator)]
	[InlineData("  +  ", QueryTokenType.LogicalOperator)]
	[InlineData("  &&  ", QueryTokenType.LogicalOperator)]
	[InlineData("  ||  ", QueryTokenType.LogicalOperator)]
	public void Flush_WithContent_AddsTrimmedTokenAndClearsBuilder(
		string termOrPhrase,
		QueryTokenType type
		)
	{
		// Assert
		var tokens = new List<ExtractedQueryToken>();
		var sb = new StringBuilder(termOrPhrase);

		var token = new ExtractedQueryToken(type, termOrPhrase.Trim());

		// Act
		_sut.Flush(sb, tokens, queryTokenType: type);

		// Assert
		Assert.Single(tokens);
		Assert.Equivalent(token, tokens[0]);
		Assert.Equal(0, sb.Length);
	}
}

