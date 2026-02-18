using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Text;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class QueryTokenizerTests
{
	private readonly QueryTokenizer _sut;

	public QueryTokenizerTests()
	{
		_sut = new QueryTokenizer();
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
	[InlineData(true)]
	[InlineData(false)]
	public void Flush_EmptyBuilder_DoesNotAddToken(bool boolean)
	{
		// Arrange
		var tokens = new List<ExtractedQueryToken>();
		var sb = new StringBuilder();

		// Act
		_sut.Flush(sb, isPhrase: boolean, tokens);

		// Assert
		Assert.Empty(tokens);
	}

	[Theory]
	[InlineData("   term   ", false, QueryTokenType.Term)]
	[InlineData("  \" phrase \"  ", true, QueryTokenType.Phrase)]
	public void Flush_WithContent_AddsTrimmedTokenAndClearsBuilder(
		string termOrPhrase, 
		bool isPhrase, 
		QueryTokenType type
		)
	{
		// Assert
		var tokens = new List<ExtractedQueryToken>();
		var sb = new StringBuilder(termOrPhrase);

		var token = new ExtractedQueryToken(type, termOrPhrase.Trim());
		
		// Act
		_sut.Flush(sb, isPhrase, tokens);

		// Assert
		Assert.Single(tokens);
		Assert.Equivalent(token, tokens[0]);
		Assert.Equal(0, sb.Length);
	}
}

