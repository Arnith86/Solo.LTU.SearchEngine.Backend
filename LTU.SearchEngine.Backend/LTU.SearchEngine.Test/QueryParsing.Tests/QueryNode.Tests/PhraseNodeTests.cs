using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using Moq;

namespace LTU.SearchEngine.Test.QueryParsing.Tests.QueryNode.Tests;

public class PhraseNodeTests
{
	[Fact]
	public void Constructor_ValidPhraseList_SetsProperty()
	{
		// Arrange
		var phraseTokens = new List<ExtractedQueryToken>
		{
			new ExtractedQueryToken(QueryTokenType.Term, "hello"),
			new ExtractedQueryToken(QueryTokenType.Term, "world")
		};

		// Act
		var node = new PhraseNode<string>(phraseTokens);

		// Assert
		Assert.Same(phraseTokens, node.Phrase);
		Assert.Equal(2, node.Phrase.Count);
	}

	[Fact]
	public void Constructor_NullPhrase_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new PhraseNode<string>(null!));
	}

	[Fact]
	public void Constructor_EmptyPhrase_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentException>(
			() => new PhraseNode<string>(new List<ExtractedQueryToken>())
		);
	}

	[Fact]
	public async Task Accept_CallsVisitOnVisitor_ReturnsExpectedValueAsync()
	{
		// Arrange
		var phraseTokens = new List<ExtractedQueryToken> { new ExtractedQueryToken(QueryTokenType.Term, "test") };
		var node = new PhraseNode<Task<HashSet<int>>>(phraseTokens);

		var mockVisitor = new Mock<IQueryVisitor<Task<HashSet<int>>>>();
		var expectedResult = Task.FromResult(new HashSet<int> { 101 });

		mockVisitor
			.Setup(v => v.VisitAsync(node))
			.ReturnsAsync(expectedResult);

		// Act
		var result = await node.AcceptAsync(mockVisitor.Object);

		// Assert
		Assert.Same(expectedResult, result);
		mockVisitor.Verify(v => v.VisitAsync(node), Times.Once);
	}
}
