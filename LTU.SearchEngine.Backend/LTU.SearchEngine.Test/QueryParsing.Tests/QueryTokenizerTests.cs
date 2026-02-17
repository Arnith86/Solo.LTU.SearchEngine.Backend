using LTU.SearchEngine.Application.QueryParsing.Helpers;
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

		// Act 
		var result = _sut.Tokenize(input);

		// Assert 
		Assert.Equal(new[] { "apple", "orange", "banana" }, result);
	}

	[Fact]
	public void Tokenize_QuotedPhrase_ReturnsPhraseAsSingleToken()
	{
		// Arrange
		var input = "cat \"hello dolly\" dog";
		// Act 
		var result = _sut.Tokenize(input);

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equal("cat", result[0]);
		Assert.Equal("hello dolly", result[1]); 
		Assert.Equal("dog", result[2]);
	}

	[Fact]
	public void Tokenize_ExtraWhitespace_ShouldBeIgnored()
	{
		// Arrange
		var input = "  word1    word2  ";
		// Act
		var result = _sut.Tokenize(input);

		// Assert
		Assert.Equal(new[] { "word1", "word2" }, result);
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
		// Act
		var result = _sut.Tokenize(input);

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equal("start", result[0]);
		Assert.Equal("\"unclosed", result[1]);
		Assert.Equal("phrase", result[2]);
	}

	[Fact]
	public void Flush_EmptyBuilder_DoesNotAddToken()
	{
		// Arrange
		var tokens = new List<string>();
		var sb = new StringBuilder();

		// Act
		_sut.Flush(sb, tokens);

		// Assert
		Assert.Empty(tokens);
	}
 
	[Fact]
	public void Flush_WithContent_AddsTrimmedTokenAndClearsBuilder()
	{
		// Assert
		var tokens = new List<string>();
		var sb = new StringBuilder("  test  ");

		// Act
		_sut.Flush(sb, tokens);

		// Assert
		Assert.Single(tokens);
		Assert.Equal("test", tokens[0]);
		Assert.Equal(0, sb.Length);
	}
}

