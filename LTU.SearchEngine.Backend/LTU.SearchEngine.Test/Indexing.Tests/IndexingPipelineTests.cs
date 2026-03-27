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
    public void Transform_GivenTitleTerm_ShouldOnlyExistInTitleTerms()
    {
        // Arrange 
        var crawlResult = CrawlResultBuilder.BuildCrawlResult(
            indexedTerms: new List<IndexedTerm> { new IndexedTerm("run", TermSource.Title)},
            extractedLinks: new List<string>()
        );
        
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("run");

        // Act 
        var document = _pipeline.Transform(crawlResult);
        

        // Assert
        Assert.True(document.TitleTerms.ContainsKey("run"));
        Assert.False(document.ContentTerms.ContainsKey("run"));
        Assert.False(document.HeaderTerms.ContainsKey("run"));
        Assert.Single(document.TitleTerms);
    }
    

    [Fact]
    public void Transform_GivenExistingTitleTerm_ShouldIncrementTitleTerms()
    {
        // Arrange 
        var crawlResult = CrawlResultBuilder.BuildCrawlResult(
            indexedTerms: new List<IndexedTerm> { 
                new IndexedTerm("run", TermSource.Title),
                new IndexedTerm("run", TermSource.Title)
            },
            extractedLinks: new List<string>()
        );
        
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("run");

        // Act 
        var document = _pipeline.Transform(crawlResult);
        

        // Assert
        Assert.Equal(2, document.TitleTerms["run"]);
    }

    
    
    [Fact]
    public void Transform_GivenHeaderTerm_ShouldOnlyExistInHeaderTerms()
    {
        // Arrange 
        var crawlResult = CrawlResultBuilder.BuildCrawlResult(
            indexedTerms: new List<IndexedTerm> { new IndexedTerm("run", TermSource.Header)},
            extractedLinks: new List<string>()
        );
        
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("run");

        // Act 
        var document = _pipeline.Transform(crawlResult);
        

        // Assert
        Assert.True(document.HeaderTerms.ContainsKey("run"));
        Assert.False(document.TitleTerms.ContainsKey("run"));
        Assert.False(document.ContentTerms.ContainsKey("run"));
        Assert.Single(document.HeaderTerms);
    }

    
    [Fact]
    public void Transform_GivenExistingHeaderTerm_ShouldIncrementHeaderTerms()
    {
        // Arrange 
        var crawlResult = CrawlResultBuilder.BuildCrawlResult(
            indexedTerms: new List<IndexedTerm> { 
                new IndexedTerm("run", TermSource.Header),
                new IndexedTerm("run", TermSource.Header)
            },
            extractedLinks: new List<string>()
        );
        
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("run");

        // Act 
        var document = _pipeline.Transform(crawlResult);
        

        // Assert
        Assert.Equal(2, document.HeaderTerms["run"]);
    }


    [Fact]
    public void Transform_GivenBodyTerm_ShouldOnlyExistInContentTerms()
    {
        // Arrange 
        var crawlResult = CrawlResultBuilder.BuildCrawlResult(
            indexedTerms: new List<IndexedTerm> { new IndexedTerm("run", TermSource.Body)},
            extractedLinks: new List<string>()
        );
        
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("run");

        // Act 
        var document = _pipeline.Transform(crawlResult);
        

        // Assert
        Assert.True(document.ContentTerms.ContainsKey("run"));
        Assert.False(document.TitleTerms.ContainsKey("run"));
        Assert.False(document.HeaderTerms.ContainsKey("run"));
        Assert.Single(document.ContentTerms);
    }


    [Fact]
    public void Transform_GivenExistingBodyTerm_ShouldIncrementBodyTerms()
    {
        // Arrange 
        var crawlResult = CrawlResultBuilder.BuildCrawlResult(
            indexedTerms: new List<IndexedTerm> { 
                new IndexedTerm("run", TermSource.Body),
                new IndexedTerm("run", TermSource.Body)
            },
            extractedLinks: new List<string>()
        );
        
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("run");

        // Act 
        var document = _pipeline.Transform(crawlResult);
        

        // Assert
        Assert.Equal(2, document.ContentTerms["run"]);
    }
    
    
    [Fact]
    public void Transform_SameWordDifferentSources_ShouldStoreSeparately()
    {
        // Arrange 
        var crawlResult = CrawlResultBuilder.BuildCrawlResult(
            indexedTerms: new List<IndexedTerm> { 
                new IndexedTerm("run", TermSource.Title),
                new IndexedTerm("run", TermSource.Header),
                new IndexedTerm("run", TermSource.Body)
            },
            extractedLinks: new List<string>()
        );
        
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("run");

        // Act 
        var document = _pipeline.Transform(crawlResult);
        

        // Assert
        Assert.Equal(1, document.TitleTerms["run"]);
        Assert.Equal(1, document.HeaderTerms["run"]);
        Assert.Equal(1, document.ContentTerms["run"]);
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

        Assert.Equal("Example Title", document.Title);

        Assert.Empty(document.TitleTerms);
        Assert.Empty(document.HeaderTerms);
        Assert.Empty(document.ContentTerms);

        _normalizerMock.Verify(n => n.Normalize(It.IsAny<string>()), Times.Never());
    }
}

