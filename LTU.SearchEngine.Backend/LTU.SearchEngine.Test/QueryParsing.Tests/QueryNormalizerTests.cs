using LTU.SearchEngine.Application.QueryParsing.Helpers;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class QueryNormalizerTests
{
	private readonly QueryNormalizer _sut;

	public QueryNormalizerTests()
	{
		_sut = new QueryNormalizer();
	}

	[Theory]
	[InlineData("HELLO", "hello")]
	[InlineData("  space  ", "space")]
	[InlineData("MixedCase", "mixedcase")]
	[InlineData("Already-low", "already-low")]
	public void NormalizeTerm_ShouldTrimAndLowercase(string input, string expected)
	{
		// Act
		var result = _sut.NormalizeTerm(input);

		// Assert
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("\"Phrase\"", "phrase")]
	[InlineData("  \"Spaced Phrase\"  ", "spaced phrase")]
	[InlineData("\"  Inner Space  \"", "inner space")]
	[InlineData("NoQuotes", "noquotes")]
	[InlineData("\"\"", "")]
	public void NormalizePhrase_ShouldRemoveQuotesAndNormalize(string input, string expected)
	{
		// Act
		var result = _sut.NormalizePhrase(input);

		// Assert
		Assert.Equal(expected, result);
	}

	[Fact]
	public void NormalizePhrase_WhenOnlyOneQuote_ShouldNotStripButLowercase()
	{
		// Edge case: A string that starts with " but doesn't end with it
		var input = "\"PartialQuote";
		var result = _sut.NormalizePhrase(input);

		Assert.Equal("\"partialquote", result);
	}
}
