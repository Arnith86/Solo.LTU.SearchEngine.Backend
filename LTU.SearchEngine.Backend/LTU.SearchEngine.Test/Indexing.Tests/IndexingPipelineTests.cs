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
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _pipeline.Transform(null!));
    }


    [Fact]
    public void Transform_GivenSingleIndexedTerm_ShouldAddNormalizedTerm()
    {
        // Arrange
        _normalizerMock
            .Setup(n => n.Normalize("Running", "en"))
            .Returns("run");

        var crawlResult = CreateCrawlResult(
            new[] { new IndexedTerm("Running", TermSource.Body) });

        // Act
        var document = _pipeline.Transform(crawlResult);

        // Assert
        Assert.Equal(1, document.ContentTerms["run"]);
        _normalizerMock.Verify(n => n.Normalize("Running", "en"), Times.Once());
    }


    [Fact]
    public void Transform_GivenSameTermInDifferentFields_ShouldKeepFieldSeparation()
    {
        // Arrange
        _normalizerMock
            .Setup(n => n.Normalize("Running", "en"))
            .Returns("run");

        var crawlResult = CreateCrawlResult( new[]
        {
            new IndexedTerm("Running", TermSource.Title),
            new IndexedTerm("Running", TermSource.Body)
        });

        // Act
        var document = _pipeline.Transform(crawlResult);

        // Assert
        Assert.Equal(1, document.TitleTerms["run"]);
        Assert.Equal(1, document.ContentTerms["run"]);

        _normalizerMock.Verify(n => n.Normalize("Running", "en"), Times.Exactly(2));
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
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
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
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
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
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
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
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
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
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
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
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
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
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
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
        // Arrange
        _normalizerMock
            .Setup(n => n.Normalize("Running", "en"))
            .Returns("run");

        _normalizerMock
            .Setup(n => n.Normalize("THE", "en"))
            .Returns((string?)null);

        var crawlResult = CreateCrawlResult(new[]
        {
            new IndexedTerm("Running", TermSource.Body),
            new IndexedTerm("THE", TermSource.Body)
        });

        // Act
        var document = _pipeline.Transform(crawlResult);

        // Assert
        Assert.Equal(1, document.ContentTerms["run"]);
        Assert.Single(document.ContentTerms);

        _normalizerMock.Verify(n => n.Normalize(It.IsAny<string>(), "en"), Times.Exactly(2));
    }


    [Fact]
    public void Transform_GivenNoIndexedTerms_ShouldReturnEmptyDocument()
    {
        // Arrange
        var crawlResult = CreateCrawlResult(Array.Empty<IndexedTerm>());

        // Act
        var document = _pipeline.Transform(crawlResult);

        // Assert 
        Assert.Equal("https://example.com", document.Url);
        Assert.Equal("Example Title", document.Title);
        Assert.Empty(document.TitleTerms);
        Assert.Empty(document.HeaderTerms);
        Assert.Empty(document.ContentTerms);

        _normalizerMock.Verify(n => n.Normalize(It.IsAny<string>(), "en"), Times.Never());
    }

    [Fact]
    public void Transform_GivenMultipleTerms_ShouldPreservePositionOrder()
    {
        // Arrange 
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
            .Returns<string, string>((s, l) => s.ToLower());
        
        var crawlResult = CreateCrawlResult(new List<IndexedTerm>
        {
            new IndexedTerm("First", TermSource.Body),
            new IndexedTerm("Second", TermSource.Body),
            new IndexedTerm("Third", TermSource.Body)
        });

        // Act 
        var document = _pipeline.Transform(crawlResult);

        // Assert
        Assert.Equal(3, document.ContentTermPositions.Count);
        Assert.Equal("first", document.ContentTermPositions[0]);
        Assert.Equal("second", document.ContentTermPositions[1]);
        Assert.Equal("third", document.ContentTermPositions[2]);
    }

    [Fact]
    public void Transform_TermsInDifferentSources_ShouldHaveSeparatePositionLists()
    {
        // Arrange 
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>(), "en"))
            .Returns<string, string>((s, l) => s.ToLower());
        
        var crawlResult = CreateCrawlResult(new List<IndexedTerm>
        {
            new IndexedTerm("title", TermSource.Title),
            new IndexedTerm("header", TermSource.Header),
            new IndexedTerm("body", TermSource.Body)
        });

        // Act 
        var document = _pipeline.Transform(crawlResult);

        // Assert
        Assert.Contains("title", document.TitleTermPositions);
        Assert.DoesNotContain("header", document.TitleTermPositions);
        Assert.DoesNotContain("body", document.TitleTermPositions);
        
        Assert.Contains("header", document.HeaderTermPositions);
        Assert.DoesNotContain("title", document.HeaderTermPositions);
        Assert.DoesNotContain("body", document.HeaderTermPositions);
        
        Assert.Contains("body", document.ContentTermPositions);
        Assert.DoesNotContain("title", document.ContentTermPositions);
        Assert.DoesNotContain("header", document.ContentTermPositions);
    }
}

