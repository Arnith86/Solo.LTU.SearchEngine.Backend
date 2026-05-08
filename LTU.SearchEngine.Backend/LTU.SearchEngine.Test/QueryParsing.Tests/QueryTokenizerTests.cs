using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.RequestParameters;
using LTU.SearchEngine.Test.HelperClasses;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class QueryTokenizerTests
{
	private readonly QueryStringTokenizer _sut;
	private readonly Mock<IQuerySyntaxHelper> _mockSyntaxHelper;
	private readonly Mock<ITextNormalizer<string, IEnumerable<string>>> _mockNormalizer;

	public QueryTokenizerTests()
	{
		_mockSyntaxHelper = new Mock<IQuerySyntaxHelper>();
		_mockNormalizer = new Mock<ITextNormalizer<string, IEnumerable<string>>>();
		_sut = new QueryStringTokenizer(_mockSyntaxHelper.Object, _mockNormalizer.Object);

        _mockNormalizer
            .Setup(n => n.Normalize(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string s, string lang) => new List<string> { s });
    }

	public SearchQueryRequestParameters CreateSearchParam(string? input, string? language)
	{
		// if (input != null && language != null)
		return SearchQueryRequestParametersBuilder.BuildParameters(input, language);
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

		var searchParam = CreateSearchParam(input, "en");
		
		// Act & Assert 
		Assert.Throws<InvalidQueryStringException>(() => _sut.Tokenize(searchParam));
	}

	[Fact]
	public void Tokenize_SimpleWords_ReturnsSeparateTokens()
	{
		// Arrange
		var input = "apple orange banana";
		
		var apple = new ExtractedQueryToken(QueryTokenType.Term, "apple", RequirementLevel.Optional, "en");
		var orange = new ExtractedQueryToken(QueryTokenType.Term, "orange", RequirementLevel.Optional, "en");
		var banana = new ExtractedQueryToken(QueryTokenType.Term, "banana", RequirementLevel.Optional, "en");

		var expected = new List<ExtractedQueryToken> { apple, orange, banana };
		var searchParam = CreateSearchParam(input, "en");

		// Act 
		var result = _sut.Tokenize(searchParam);

		// Assert 
		Assert.Equivalent(expected, result.Tokens);
	}

	[Fact]
	public void Tokenize_QuotedPhrase_ReturnsPhraseAsSingleToken()
	{
		// Arrange
		var input = "cat \"hello dolly\" dog";

		var cat = new ExtractedQueryToken(QueryTokenType.Term, "cat", RequirementLevel.Optional, "en");
		var helloDolly = new ExtractedQueryToken(QueryTokenType.Phrase, "hello dolly", RequirementLevel.Optional, "en");
		var dog = new ExtractedQueryToken(QueryTokenType.Term, "dog", RequirementLevel.Optional, "en");

		var searchParam = CreateSearchParam(input, "en");

		// Act 
		var result = _sut.Tokenize(searchParam);
		var resultList = result.Tokens.ToList();

		// Assert
		Assert.Equal(5, resultList.Count);
		Assert.Equivalent(cat, resultList[0]);
		Assert.Equivalent(helloDolly, resultList[2]);
		Assert.Equivalent(dog, resultList[4]);
	}

	[Fact]
	public void Tokenize_ExtraWhitespace_ShouldBeIgnored()
	{
		// Arrange
		var input = "  word1    word2  ";
		var word1 = new ExtractedQueryToken(QueryTokenType.Term, "word1", RequirementLevel.Optional, "en");
		var word2 = new ExtractedQueryToken(QueryTokenType.Term, "word2", RequirementLevel.Optional, "en");
		var searchParam = CreateSearchParam(input, "en");

		// Act
		var result = _sut.Tokenize(searchParam);

		// Assert
		Assert.Equivalent(new[] { word1, word2}, result.Tokens);
	}


	[Fact]
	public void Tokenize_EmptyInput_ReturnsEmptyList()
	{
		// Act & Assert 
		Assert.Empty(_sut.Tokenize(CreateSearchParam("", "en")).Tokens);
		Assert.Empty(_sut.Tokenize(CreateSearchParam("   ", "en")).Tokens);
	}


	[Fact]
	public void Tokenize_UnclosedQuotes_TreatsAllWordsAsTerms_AddsImplicitOr()
	{
		// Arrange
		var input = "start \"unclosed phrase";

		var start = new ExtractedQueryToken(QueryTokenType.Term, "start", RequirementLevel.Optional, "en");
		var unclosed = new ExtractedQueryToken(QueryTokenType.Term, "\"unclosed", RequirementLevel.Optional, "en");
		var phrase = new ExtractedQueryToken(QueryTokenType.Term, "phrase", RequirementLevel.Optional, "en");
		var or = new ExtractedQueryToken(QueryTokenType.LogicalOperator, "OR", RequirementLevel.Optional);
		
		var searchParam = CreateSearchParam(input, "en");

		// Act
		var result = _sut.Tokenize(searchParam);
		var resultList = result.Tokens.ToList();

		// Assert
		Assert.Equal(5, resultList.Count);
		Assert.Equivalent(start, resultList[0]);
		Assert.Equivalent(or, resultList[1]);
		Assert.Equivalent(unclosed, resultList[2]);
		Assert.Equivalent(or, resultList[3]);
		Assert.Equivalent(phrase, resultList[4]);
	}

    [Fact]
    public void Tokenize_TwoTermsSeparatedBySpace_InsertsImplicitOr()
    {
        // Arrange
        var input = "apple banana";
        var searchParam = CreateSearchParam(input, "en");

		// Act 
		var result = _sut.Tokenize(searchParam);


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
        var searchParam = CreateSearchParam(input, "en");

		// Act 
		var result = _sut.Tokenize(searchParam);

		// Assert 
		var resultList = result.Tokens.ToList();

        Assert.Equal(5, resultList.Count);

        Assert.Equal("apple", resultList[0].Token);
        Assert.Equal("OR", resultList[1].Token);
        Assert.Equal("banana", resultList[2].Token);
        Assert.Equal("OR", resultList[3].Token);
        Assert.Equal("orange", resultList[4].Token);
    }


	[Theory]
    [InlineData("+apple", "apple")]
    [InlineData("+orange", "orange")]
    public void Tokenize_PlusPrefixOnTerm_SetsRequirementLevelToRequired(string input, string expectedTerm)
    {
		// Arrange 
		var searchParam = CreateSearchParam(input, "en");

        // Act
        var result = _sut.Tokenize(searchParam);
        var token = result.Tokens.First();

        // Assert
        Assert.Equal(expectedTerm, token.Token);
        Assert.Equal(RequirementLevel.Required, token.RequirementLevel);
        Assert.Equal(QueryTokenType.Term, token.TokenType);
    }


	[Fact]
    public void Tokenize_PlusPrefixOnPhrase_SetsRequirementLevelToRequired()
    {
        // Arrange
        var input = "+\"mandatory phrase\"";
		var searchParam = CreateSearchParam(input, "en");

        // Act
        var result = _sut.Tokenize(searchParam);
        var token = result.Tokens.First();

        // Assert
        Assert.Equal("mandatory phrase", token.Token);
        Assert.Equal(RequirementLevel.Required, token.RequirementLevel);
        Assert.Equal(QueryTokenType.Phrase, token.TokenType);
    }


	[Theory]
	[InlineData("+apple +banana")]
	[InlineData("+\"apple pie\" +\"banana split\"")]
    public void Tokenize_MultipleRequiredTerms_InsertsImplicitAND(string input)
    {
		// Arrange
		var searchParam = CreateSearchParam(input, "en");
		
        // Act
        var result = _sut.Tokenize(searchParam);
        var resultList = result.Tokens.ToList();

        // Assert (required, AND, required)
        Assert.Equal(3, resultList.Count);
        Assert.Equal(RequirementLevel.Required, resultList[0].RequirementLevel);
        Assert.Equal(RequirementLevel.Required, resultList[2].RequirementLevel);
        Assert.Equal("AND", resultList[1].Token);
    }


	[Theory]
	[InlineData("\\+apple", "+apple")]
	[InlineData("\\+\"apple banana\"", "+apple banana")]
	public void Tokenize_EscapedPlusAtStart_ShouldBeOptional(string input, string expected)
    {
		// Arrange 
		var searchParam = CreateSearchParam(input, "en");

        // Act
        var result = _sut.Tokenize(searchParam);
        var token = result.Tokens.First();

        // Assert
        Assert.Equal(expected, token.Token);
        Assert.Equal(RequirementLevel.Optional, token.RequirementLevel);
    }
	
	
	[Theory]
	[InlineData("\"apple \\+ banana\"", "apple + banana")]
	[InlineData("\"\\+apple  banana\"", "+apple banana")]
	[InlineData("\"apple \\+banana\"", "apple +banana")]
	[InlineData("\"apple banana\\+\"", "apple banana+")]
	public void Tokenize_EscapedPlusInPhrase_ShouldNotInsertRequired(string input, string expected)
    {
		// Arrange
		var searchParam = CreateSearchParam(input, "en");

        // Act
        var result = _sut.Tokenize(searchParam);
        var token = result.Tokens.First();

        // Assert
        Assert.Equal(expected, token.Token);
        Assert.Equal(RequirementLevel.Optional, token.RequirementLevel);
    }


	[Fact]
	public void Tokenize_RequiredGrouping_StartGroupingOperatorTaggedAsRequired()
    {
		// Arrange
		string input = "+(term1 AND term2)";
		var searchParam = CreateSearchParam(input, "en");
		
        // Act
        var result = _sut.Tokenize(searchParam);
        var token = result.Tokens.First();

        // Assert
        Assert.Equal("(", token.Token);
        Assert.Equal(RequirementLevel.Required, token.RequirementLevel);
    }


	[Fact]
	public void Tokenize_NestedRequiredGrouping_InnerAndOuterParenthesesTaggedAsRequired()
	{
		// Arrange
		// Logic: +( term1 AND +( term1 OR term2 ) )
		string input = "+(term1 AND +(term1 OR term2))";
		var searchParam = CreateSearchParam(input, "en");
		
		// Act
		var result = _sut.Tokenize(searchParam);
		var tokens = result.Tokens.ToList();

		// Assert
		// 1. Check Outer Parenthesis: +(
		var outerParen = tokens[0];
		Assert.Equal("(", outerParen.Token);
		Assert.Equal(RequirementLevel.Required, outerParen.RequirementLevel);

		// 2. Check Inner Parenthesis: Sequence should be: 0:+(  1:term1, 2:AND, 3:+(
		var innerParen = tokens[3];
		Assert.Equal("(", innerParen.Token);
		Assert.Equal(RequirementLevel.Required, innerParen.RequirementLevel);
	}


	[Fact]
	public void Tokenize_DeeplyNestedRequired_AllOpeningBracketsAreRequired()
	{
		// Arrange
		string input = "+(+(+(term)))"; // Triple nested required
		var searchParam = CreateSearchParam(input, "en");

		// Act 
		var result = _sut.Tokenize(searchParam);

		var openBrackets = result.Tokens.Where(t => t.Token == "(").ToList();

		// Assert
		Assert.Equal(3, openBrackets.Count);
		Assert.All(openBrackets, t => Assert.Equal(RequirementLevel.Required, t.RequirementLevel));
	}


    [Fact]
    public void Tokenize_TermFollowedByExplicitOperator_DoesNotInsertImplicitOr()
    {
		// Arrange
		var input = "start AND phrase";
		var searchParam = CreateSearchParam(input, "en");

		// Act 
        var result = _sut.Tokenize(searchParam);

		// Assert 
		var resultList = result.Tokens.ToList();

        Assert.Equal(3, resultList.Count);

        Assert.Equal("start", resultList[0].Token);
        Assert.Equal("AND", resultList[1].Token);
        Assert.Equal("phrase", resultList[2].Token);
    }

    [Fact]
    public void Tokenize_TermPhraseTerm_DoesNotInsertImplicitOrInPhrase()
    {
		// Arrange 
        var input = "cat \"hello dolly\" dog";
		var searchParam = CreateSearchParam(input, "en");

		// Act 
        var result = _sut.Tokenize(searchParam);

		// Assert 
		var resultList = result.Tokens.ToList();
		
        Assert.Equal(5, resultList.Count);
        Assert.Equal(QueryTokenType.Term, resultList[0].TokenType);
		Assert.Equal(QueryTokenType.LogicalOperator, resultList[1].TokenType);
        Assert.Equal(QueryTokenType.Phrase, resultList[2].TokenType);
		Assert.Equal(QueryTokenType.LogicalOperator, resultList[3].TokenType);
        Assert.Equal(QueryTokenType.Term, resultList[4].TokenType);
    }

    [Fact]
    public void Tokenize_TermFollowedBySymbolicOperator_DoesNotInsertImplicitOr()
    {
		// Arrange
		var input = "start && phrase";
		var searchParam = CreateSearchParam(input, "en");

		// Act 
        var result = _sut.Tokenize(searchParam);

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
	[InlineData("&&")]
	[InlineData("||")]
	[InlineData("AND")]
	[InlineData("OR")]
	[InlineData("NOT")]
	public void Tokenize_Operators_HandledAsLogicalOperators(string operatorInput)
	{
		// Arrange
		var input = $"start {operatorInput} phrase";
		var searchParam = CreateSearchParam(input, "en");

		var start = new ExtractedQueryToken(QueryTokenType.Term, "start", RequirementLevel.Optional, "en");
		var expectedOperator = new ExtractedQueryToken(QueryTokenType.LogicalOperator, operatorInput, RequirementLevel.Optional);
		var phrase = new ExtractedQueryToken(QueryTokenType.Term, "phrase", RequirementLevel.Optional, "en");

		// Act
		var result = _sut.Tokenize(searchParam);
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
		var searchParam = CreateSearchParam(input, "en");

		var first = new ExtractedQueryToken(QueryTokenType.Term, "first", RequirementLevel.Optional, "en");
		var expectedOperator = new ExtractedQueryToken(
			QueryTokenType.LogicalOperator, operatorInput, RequirementLevel.Optional, "Unknown"
		);
		var second = new ExtractedQueryToken(QueryTokenType.Term, "second", RequirementLevel.Optional, "en");

		// Act
		var result = _sut.Tokenize(searchParam);

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
		var searchParam = CreateSearchParam(input, "en");

		var expectedOperator1 = new ExtractedQueryToken(
			QueryTokenType.GroupingOperator, operator1, RequirementLevel.Optional
		);
		var expectedPhrase = new ExtractedQueryToken(
			QueryTokenType.Phrase, "start phrase", RequirementLevel.Optional, "en"
		);
		var expectedOperator2 = new ExtractedQueryToken(
			QueryTokenType.GroupingOperator, operator2, RequirementLevel.Optional
		);

		// Act
		var result = _sut.Tokenize(searchParam);

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
            .Returns((string s, string lang) => new List<string> { s });

		var searchParam = CreateSearchParam("the Running man", "en");
		// Act 
        _sut.Tokenize(searchParam);

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
    [InlineData("-")]
    public void Tokenize_Should_Not_Normalize_LogicalOperators(string input)
    {
        // Arrange
		var searchParam = CreateSearchParam(input, "en");

		// Act 
        var result = _sut.Tokenize(searchParam);
		var resultList = result.Tokens.ToList();

		// Assert 
        Assert.Single(resultList);
        Assert.Equal(QueryTokenType.LogicalOperator, resultList[0].TokenType);
		
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
		// Arrange
		var searchParam = CreateSearchParam(input, "en");

		// Act 
		var result = _sut.Tokenize(searchParam);
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
		// Arrange 
		var searchParam = CreateSearchParam(input, "en");

		// Act 
		var result = _sut.Tokenize(searchParam);
		var resultList = result.Tokens.ToList();

		// Assert 
		var englishTokens = resultList.Where(est => est.Language.Equals("en"));

		Assert.Single(englishTokens);
	}
	
	[Theory]
	[InlineData("term1", 1)]
	[InlineData("\"term1 term2\"", 1)]
	[InlineData("term1 \"term2 term3\"", 2)] 
	[InlineData("term1 AND term3", 2)] 
	public void Tokenize_IsLanguagePreFix_FindsNoLanguage_UsesDefault(string input, int instances)
	{
		// Arrange 
		var searchParam = CreateSearchParam(input, "en");

		// Act 
		var result = _sut.Tokenize(searchParam);
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
			.Returns(new List<string>{""}); // Everything is a stop-word

		var input = "\"the and a\"";
		var searchParam = CreateSearchParam(input, "en");

		// Act
		var result = _sut.Tokenize(searchParam);

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
		var searchParam = CreateSearchParam(input, "en");

		// Act
		var result = _sut.Tokenize(searchParam);
		var resultList = result.Tokens.ToList();

		// Assert
		var apple = resultList.First(t => t.Token == "apple");
		var banan = resultList.First(t => t.Token == "banan");
		var orange = resultList.First(t => t.Token == "orange");

		Assert.Equal("en", apple.Language);
		Assert.Equal("sv", banan.Language);
		Assert.Equal("en", orange.Language);
	}

	[Fact]
	public void Flush_WhenWordIsNormalizedToEmpty_ShouldStillAddTokenAndTrackIgnored()
	{
		// Arrange
		_mockNormalizer.Setup(n => n.Normalize("the", "en")).Returns(new List<string>{""});
		var input = "the";
		var searchParam = CreateSearchParam(input, "en");

		// Act
		var result = _sut.Tokenize(searchParam);

		// Assert
		Assert.Single(result.Tokens); 
		Assert.Equal(string.Empty, result.Tokens.First().Token);
		
		Assert.Single(result.IgnoredTokens);
		Assert.Equal("the", result.IgnoredTokens.First().Token);
	}

	[Theory]
	[InlineData("A\\+\\+", "A++")]
	[InlineData("\\!B", "!B")]
	[InlineData("\\-C\\+", "-C+")]
	[InlineData("D\\+E", "D+E")]
	[InlineData("\\-F\\+G\\#H", "-F+G#H")]
	public void Tokenize_EscapedSymbols_ArePassedToNormalizerAsLiteralText(string input, string expected)
	{
		// Arrange 
		var searchParam = CreateSearchParam(input, "en");
		
		// Act
		var result = _sut.Tokenize(searchParam);

		// Assert
		Assert.Equal(expected, result.Tokens.First().Token); 
		_mockNormalizer.Verify(n => n.Normalize(expected, "en"), Times.Once);
	}
}

