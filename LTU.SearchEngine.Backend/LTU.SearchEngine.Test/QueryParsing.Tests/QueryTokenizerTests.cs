using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class QueryTokenizerTests
{
	private readonly QueryStringTokenizer _sut;
	private readonly Mock<IQuerySyntaxHelper> _mockSyntaxHelper;
	private readonly Mock<ITextNormalizer<string>> _mockNormalizer;

	public QueryTokenizerTests()
	{
		_mockSyntaxHelper = new Mock<IQuerySyntaxHelper>();
		_mockNormalizer = new Mock<ITextNormalizer<string>>();
		_sut = new QueryStringTokenizer(_mockSyntaxHelper.Object, _mockNormalizer.Object);

        _mockNormalizer
            .Setup(n => n.Normalize(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string s, string lang) => s);
    }

	[Fact]
	public void Constructor_NullIQuerySyntaxHelper_ShouldThrowArgumentNullException()
	{
        // Act & Assert 
        Assert.Throws<ArgumentNullException>(() =>
			new QueryStringTokenizer(
				new Mock<IQuerySyntaxHelper>().Object,
				null!
			)
		);
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
		Assert.Throws<InvalidQueryStringException>(() => _sut.Tokenize(input, "en"));
	}

	[Fact]
	public void Tokenize_SimpleWords_ReturnsSeparateTokens()
	{
		// Arrange
		var input = "apple orange banana";
		
		var apple = new ExtractedQueryToken(QueryTokenType.Term, "apple", "en");
		var orange = new ExtractedQueryToken(QueryTokenType.Term, "orange", "en");
		var banana = new ExtractedQueryToken(QueryTokenType.Term, "banana", "en");

		var expected = new List<ExtractedQueryToken> { apple, orange, banana };

		// Act 
		var result = _sut.Tokenize(input, "en");

		// Assert 
		Assert.Equivalent(expected, result.Tokens);
	}

	[Fact]
	public void Tokenize_QuotedPhrase_ReturnsPhraseAsSingleToken()
	{
		// Arrange
		var input = "cat \"hello dolly\" dog";

		var cat = new ExtractedQueryToken(QueryTokenType.Term, "cat", "en");
		var helloDolly = new ExtractedQueryToken(QueryTokenType.Phrase, "hello dolly", "en");
		var dog = new ExtractedQueryToken(QueryTokenType.Term, "dog", "en");

		// Act 
		var result = _sut.Tokenize(input, "en");
		var resultList = result.Tokens.ToList();

		// Assert
		Assert.Equal(3, resultList.Count);
		Assert.Equivalent(cat, resultList[0]);
		Assert.Equivalent(helloDolly, resultList[1]);
		Assert.Equivalent(dog, resultList[2]);
	}

	[Fact]
	public void Tokenize_ExtraWhitespace_ShouldBeIgnored()
	{
		// Arrange
		var input = "  word1    word2  ";
		var word1 = new ExtractedQueryToken(QueryTokenType.Term, "word1", "en");
		var word2 = new ExtractedQueryToken(QueryTokenType.Term, "word2", "en");

		// Act
		var result = _sut.Tokenize(input, "en");

		// Assert
		Assert.Equivalent(new[] { word1, word2}, result.Tokens);
	}


	[Fact]
	public void Tokenize_EmptyInput_ReturnsEmptyList()
	{
		// Act & Assert 
		Assert.Empty(_sut.Tokenize("", "en").Tokens);
		Assert.Empty(_sut.Tokenize("   ", "en").Tokens);
	}


	[Fact]
	public void Tokenize_UnclosedQuotes_TreatsAllWordsAsTerms()
	{
		// Arrange
		var input = "start \"unclosed phrase";

		var start = new ExtractedQueryToken(QueryTokenType.Term, "start", "en");
		var unclosed = new ExtractedQueryToken(QueryTokenType.Term, "\"unclosed", "en");
		var phrase = new ExtractedQueryToken(QueryTokenType.Term, "phrase", "en");

		// Act
		var result = _sut.Tokenize(input, "en");
		var resultList = result.Tokens.ToList();

		// Assert
		Assert.Equal(3, resultList.Count);
		Assert.Equivalent(start, resultList[0]);
		Assert.Equivalent(unclosed, resultList[1]);
		Assert.Equivalent(phrase, resultList[2]);
	}

    [Fact]
    public void Tokenize_TwoTermsSeparatedBySpace_InsertsImplicitOr()
    {
        // Arrange
        var input = "apple banana";
        
		// Act 
		var result = _sut.Tokenize(input, "en");


		// Assert 
		var resultList = result.Tokens.ToList();
        
		Assert.Equal(3, resultList.Count);
        Assert.Equal(QueryTokenType.Term, resultList[0].TokenType);
        Assert.Equal("apple", resultList[0].Token);

        Assert.Equal(QueryTokenType.LogicalOperator, resultList[1].TokenType);
        Assert.Equal("OR", resultList[1].Token);

        Assert.Equal(QueryTokenType.Term, resultList[2].TokenType);
        Assert.Equal("banana", resultList[2].Token);
    }

    [Fact]
    public void Tokenize_MultipleTermsSeparatedBySpaces_InsertsImplicitOrBetweenAll()
    {
		// Arrange
		var input = "apple banana orange";
        
		// Act 
		var result = _sut.Tokenize(input, "en");

		// Assert 
		var resultList = result.Tokens.ToList();

        Assert.Equal(5, resultList.Count);

        Assert.Equal("apple", resultList[0].Token);
        Assert.Equal("OR", resultList[1].Token);
        Assert.Equal("banana", resultList[2].Token);
        Assert.Equal("OR", resultList[3].Token);
        Assert.Equal("orange", resultList[4].Token);
    }

    [Fact]
    public void Tokenize_TermFollowedByExplicitOperator_DoesNotInsertImplicitOr()
    {
		// Arrange
		var input = "start AND phrase";

		// Act 
        var result = _sut.Tokenize(input, "en");

		// Assert 
		var resultList = result.Tokens.ToList();

        Assert.Equal(3, resultList.Count);

        Assert.Equal("start", resultList[0].Token);
        Assert.Equal("AND", resultList[1].Token);
        Assert.Equal("phrase", resultList[2].Token);
    }

    [Fact]
    public void Tokenize_TermPhraseTerm_DoesNotInsertImplicitOr()
    {
		// Arrange 
        var input = "cat \"hello dolly\" dog";

		// Act 
        var result = _sut.Tokenize(input, "en");

		// Assert 
		var resultList = result.Tokens.ToList();
		
        Assert.Equal(3, resultList.Count);
        Assert.Equal(QueryTokenType.Term, resultList[0].TokenType);
        Assert.Equal(QueryTokenType.Phrase, resultList[1].TokenType);
        Assert.Equal(QueryTokenType.Term, resultList[2].TokenType);
    }

    [Fact]
    public void Tokenize_UnclosedQuote_DoesNotInsertImplicitOr()
    {
		// Arrange 
		var input = "start \"unclosed phrase";
        
		// Act 
		var result = _sut.Tokenize(input, "en");

		// Assert 
		var resultList = result.Tokens.ToList();

        Assert.Equal(3, resultList.Count);

        Assert.Equal("start", resultList[0].Token);
        Assert.Equal("\"unclosed", resultList[1].Token);
        Assert.Equal("phrase", resultList[2].Token);
    }

    [Fact]
    public void Tokenize_TermFollowedBySymbolicOperator_DoesNotInsertImplicitOr()
    {
		// Arrange
		var input = "start && phrase";

		// Act 
        var result = _sut.Tokenize(input, "en");

		// Assert 
		var resultList = result.Tokens.ToList();
        
		Assert.Equal(3, resultList.Count);

        Assert.Equal("start", resultList[0].Token);
        Assert.Equal("&&", resultList[1].Token);
        Assert.Equal("phrase", resultList[2].Token);
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

		var start = new ExtractedQueryToken(QueryTokenType.Term, "start", "en");
		var expectedOperator = new ExtractedQueryToken(QueryTokenType.LogicalOperator, operatorInput, "en");
		var phrase = new ExtractedQueryToken(QueryTokenType.Term, "phrase", "en");

		// Act
		var result = _sut.Tokenize(input, "en");
		var resultList = result.Tokens.ToList();

		// Assert
		Assert.Equal(3, resultList.Count);
		Assert.Equivalent(start, resultList[0]);
		Assert.Equivalent(expectedOperator, resultList[1]);
		Assert.Equivalent(phrase, resultList[2]);
		Assert.Equal(expectedOperator.TokenType, resultList[1].TokenType);
	}

	[Theory]
	[InlineData("!")]
	[InlineData("-")]
	public void Tokenize_NotWithoutSpace_LogicalOperatorSeparateFromTerm(string operatorInput)
	{
		// Arrange
		var input = $"first {operatorInput}second";

		var first = new ExtractedQueryToken(QueryTokenType.Term, "first", "en");
		var expectedOperator = new ExtractedQueryToken(QueryTokenType.LogicalOperator, operatorInput, "en");
		var second = new ExtractedQueryToken(QueryTokenType.Term, "second", "en");

		// Act
		var result = _sut.Tokenize(input, "en");

		// Assert
		var resultList = result.Tokens.ToList();

		Assert.Equal(3, resultList.Count);
		Assert.Equivalent(first, resultList[0]);
		Assert.Equivalent(expectedOperator, resultList[1]);
		Assert.Equivalent(second, resultList[2]);
		Assert.Equal(expectedOperator.TokenType, resultList[1].TokenType);
	}

	[Theory]
	[InlineData("(", ")")]
	[InlineData("{", "}")]
	[InlineData("[", "]")]
	public void Tokenize_Operators_HandledAsGroupingOperators(string operator1, string operator2)
	{
		// Arrange
		var input = $"{operator1}\"start phrase\"{operator2}";

		var expectedOperator1 = new ExtractedQueryToken(QueryTokenType.GroupingOperator, operator1, "en");
		var expectedPhrase = new ExtractedQueryToken(QueryTokenType.Phrase, "start phrase", "en");
		var expectedOperator2 = new ExtractedQueryToken(QueryTokenType.GroupingOperator, operator2, "en");

		// Act
		var result = _sut.Tokenize(input, "en");

		// Assert
		var resultList = result.Tokens.ToList();

		Assert.Equal(3, resultList.Count);
		Assert.Equivalent(expectedOperator1, resultList[0]);
		Assert.Equivalent(expectedPhrase, resultList[1]);
		Assert.Equivalent(expectedOperator2, resultList[2]);
		Assert.Equal(expectedOperator1.TokenType, resultList[0].TokenType);
		Assert.Equal(expectedOperator2.TokenType, resultList[2].TokenType);
	}


    [Fact]
    public void Tokenize_Should_Call_Normalizer_For_Term()
    {
		// Arrange 
        _mockNormalizer
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
            .Returns((string s, string lang) => s);

		// Act 
        _sut.Tokenize("the Running man", "en");

		// Assert
        _mockNormalizer.Verify(
            n => n.Normalize(It.IsAny<string>(), "en"),
            Times.Exactly(3));
    }

    [Theory]
    [InlineData("AND")]
    [InlineData("OR")]
    [InlineData("NOT")]
    [InlineData("!")]
    [InlineData("||")]
    [InlineData("&&")]
    [InlineData("+")]
    [InlineData("-")]
    public void Tokenize_Should_Not_Normalize_LogicalOperators(string input)
    {
        // Arrange
        var result = _sut.Tokenize(input, "en");
		var resultList = result.Tokens.ToList();

		// Act 
        Assert.Single(resultList);
        Assert.Equal(QueryTokenType.LogicalOperator, resultList[0].TokenType);
		
		// Assert 
        _mockNormalizer.Verify(
            n => n.Normalize(It.IsAny<string>(), "en"),
            Times.Never);
    }

	
	[Theory]
	[InlineData("en: term1")]
	[InlineData("en: term1 term2")]
	[InlineData("en: \"term1 term2\"")]
	[InlineData("en: term1 term2 term3")]
	public void Tokenize_IsLanguagePreFix_FindsGlobalLanguage(string input)
	{
		// Act 
		var result = _sut.Tokenize(input, "sv");
		var resultList = result.Tokens.ToList();

		// Assert 
		var swedishTokens = resultList.Where(est => est.Language.Equals("sv"));

		Assert.Empty(swedishTokens);
	}
	
	
	[Theory]
	[InlineData("en:term1")]
	[InlineData("sv: term1 en:term2")]
	[InlineData("en:\"term1 term2\"")]
	[InlineData("term1 term2 en:term3")]
	public void Tokenize_IsLanguagePreFix_FindsActiveLanguage(string input)
	{
		// Act 
		var result = _sut.Tokenize(input, "sv");
		var resultList = result.Tokens.ToList();

		// Assert 
		var englishTokens = resultList.Where(est => est.Language.Equals("en"));

		Assert.Single(englishTokens);
	}
	
	[Theory]
	[InlineData("term1", 1)]
	[InlineData("\"term1 term2\"", 1)]
	[InlineData("term1 \"term2 term3\"", 2)]
	[InlineData("term1 en:AND term3", 2)]
	[InlineData("term1 AND term3", 3)]
	public void Tokenize_IsLanguagePreFix_FindsNoLanguage_UsesDefault(string input, int instances)
	{
		// Act 
		var result = _sut.Tokenize(input, "sv");
		var resultList = result.Tokens.ToList();

		// Assert 
		var swedishTokens = resultList.Where(est => est.Language.Equals("sv"));

		Assert.Equal(instances, swedishTokens.Count());
	}

	[Fact]
	public void Tokenize_PhraseWithOnlyStopWords_CreatesSingleEmptyToken()
	{
		// Arrange
		_mockNormalizer
			.Setup(n => n.Normalize(It.IsAny<string>(), "en"))
			.Returns(""); // Everything is a stop-word

		var input = "\"the and a\"";

		// Act
		var result = _sut.Tokenize(input, "en");

		// Assert
		var resultList = result.Tokens.ToList();

		Assert.Single(resultList);
		Assert.Equal("", resultList.First().Token);
	}

	[Fact]
	public void Tokenize_MixedLanguagesInSameQuery_AssignsCorrectLanguages()
	{
		// Arrange
		var input = "en:apple sv:banan orange"; 
		// en:apple -> English
		// sv:banan -> Swedish
		// orange   -> English (falls back to global)

		// Act
		var result = _sut.Tokenize(input, "en");
		var resultList = result.Tokens.ToList();

		// Assert
		var apple = resultList.First(t => t.Token == "apple");
		var banan = resultList.First(t => t.Token == "banan");
		var orange = resultList.First(t => t.Token == "orange");

		Assert.Equal("en", apple.Language);
		Assert.Equal("sv", banan.Language);
		Assert.Equal("en", orange.Language);
	}
}

