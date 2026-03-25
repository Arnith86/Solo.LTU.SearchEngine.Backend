using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Test.HelperClasses;
using Moq;
using System.Net;

namespace LTU.SearchEngine.Test.Indexing.Tests;

public class IndexingPipelineTests
{
    private readonly Mock<ITextNormalizer<string>> _normalizerMock;
    private readonly IndexingPipeline _pipeline;

    public IndexingPipelineTests()
    {
        _normalizerMock = new Mock<ITextNormalizer<string>>();
        _pipeline = new IndexingPipeline(_normalizerMock.Object);
    }

    private CrawlResult CreateCrawlResult(IEnumerable<IndexedTerm> indexedTerms)
    {
        return CrawlResultBuilder.BuildCrawlResult(
            url: "https://example.com",
            title: "Example Title",
            language: "en",
            indexedTerms: indexedTerms,
            type: "text/html",
            content: "x",
            extractedLinks: Array.Empty<string>(),
            statusCode: HttpStatusCode.OK,
            timeTakenMs: 0,
            hashContent: "FakeHash"
        );
    }

    [Fact]
    public void Transform_GivenNullCrawlResult_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _pipeline.Transform(null!));
    }

    [Fact]
    public void Transform_GivenSingleIndexedTerm_ShouldAddNormalizedTerm()
    {
        // Arrange
        _normalizerMock
            .Setup(n => n.Normalize("Running"))
            .Returns("run");

        var crawlResult = CreateCrawlResult(
            new[] { new IndexedTerm("Running", TermSource.Body) });

        // Act
        var document = _pipeline.Transform(crawlResult);

        // Assert
        Assert.Equal(1, document.ContentTerms["run"]);
        _normalizerMock.Verify(n => n.Normalize("Running"), Times.Once());
    }

    [Fact]
    public void Transform_GivenSameTermInDifferentFields_ShouldKeepFieldSeparation()
    {
        _normalizerMock
            .Setup(n => n.Normalize("Running"))
            .Returns("run");

        var crawlResult = CreateCrawlResult(
            new[]
            {
                new IndexedTerm("Running", TermSource.Title),
                new IndexedTerm("Running", TermSource.Body)
            });

        var document = _pipeline.Transform(crawlResult);

        Assert.Equal(1, document.TitleTerms["run"]);
        Assert.Equal(1, document.ContentTerms["run"]);

        _normalizerMock.Verify(n => n.Normalize("Running"), Times.Exactly(2));
    }

    [Fact]
    public void Transform_GivenNullNormalizedTerm_ShouldSkipTerm()
    {
        _normalizerMock
            .Setup(n => n.Normalize("Running"))
            .Returns("run");

        _normalizerMock
            .Setup(n => n.Normalize("THE"))
            .Returns((string?)null);

        var crawlResult = CreateCrawlResult(
            new[]
            {
                new IndexedTerm("Running", TermSource.Body),
                new IndexedTerm("THE", TermSource.Body)
            });

        var document = _pipeline.Transform(crawlResult);

        Assert.Equal(1, document.ContentTerms["run"]);
        Assert.Single(document.ContentTerms);

        _normalizerMock.Verify(n => n.Normalize(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public void Transform_GivenNoIndexedTerms_ShouldReturnEmptyDocument()
    {
        var crawlResult = CreateCrawlResult(Array.Empty<IndexedTerm>());

        var document = _pipeline.Transform(crawlResult);

        Assert.Equal("https://example.com", document.Url);

        // DocId is generated Guid
        Assert.NotNull(document.DocId);
        Assert.True(Guid.TryParse(document.DocId, out _));

        Assert.Equal("Example Title", document.Title);

        Assert.Empty(document.TitleTerms);
        Assert.Empty(document.HeaderTerms);
        Assert.Empty(document.ContentTerms);

        _normalizerMock.Verify(n => n.Normalize(It.IsAny<string>()), Times.Never());
    }
}

