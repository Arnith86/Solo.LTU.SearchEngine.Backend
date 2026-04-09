using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Infrastructure.Repositories;
using LTU.SearchEngine.Test.HelperClasses;
using Moq;
using System.Net;
using IIndexingPipeline = LTU.SearchEngine.Backend.Core.Model.IIndexingPipeline; 

namespace LTU.SearchEngine.Test.Indexing.IndexerTests;

public class IndexerTests
{
    private readonly Mock<IIndexRepository> _repositoryMock;
    private readonly Mock<IIndexingPipeline> _pipelineMock;
    private readonly Indexer _sut;

    public IndexerTests()
    {
        _repositoryMock = new Mock<IIndexRepository>();
        _pipelineMock = new Mock<IIndexingPipeline>();

        _sut = new Indexer(
            repository: _repositoryMock.Object,
            pipeline: _pipelineMock.Object
        );
    }

    private CrawlResult CreateDummyCrawlResult()
    {
        return CrawlResultBuilder.BuildCrawlResult(
            url: "https://test.com",
            title: "Test",
            language: "en",
            indexedTerms: new List<IndexedTerm>{ new IndexedTerm("engine", TermSource.Title)},
            type: "text/html",
            content: "x",
            extractedLinks: new List<string>(),
            statusCode: HttpStatusCode.OK,
            timeTakenMs: 10,
            hashContent: "FakeHash"
        );
    }

    [Fact]
    public async Task IndexAsync_ShouldThrow_WhenCrawlResultIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.IndexAsync(null!));
    }

    [Fact]
    public async Task IndexAsync_ShouldCallPipeline_WhenValidInput()
    {
        var crawlResult = CreateDummyCrawlResult();

        _pipelineMock
            .Setup(p => p.Transform(crawlResult))
            .Returns(IndexDocumentBuilder.BuildIndexDocument());

        // Act
        await _sut.IndexAsync(crawlResult);

        // Assert
        _pipelineMock.Verify(p => p.Transform(crawlResult), Times.Once);
    }


    [Fact]
    public async Task IndexAsync_ShouldSaveTransformedDocument()
    {
        var crawlResult = CreateDummyCrawlResult();

        var indexDocument = IndexDocumentBuilder.BuildIndexDocument();

        _pipelineMock
            .Setup(p => p.Transform(crawlResult))
            .Returns(indexDocument); 

        // Act
        await _sut.IndexAsync(crawlResult); 

        // Assert
        _repositoryMock.Verify(r => r.AddDocumentAsync(indexDocument), Times.Once);
    }


    [Fact]
    public async Task GetExistingDocumentIdAsync_ShouldReturnId_WhenHashExists()
    {
        // Arrange
        var hash = "SomeHash";
        int expectedId = 123;
        
        _repositoryMock
            .Setup(r => r.GetExistingDocumentByHashAsync(hash))
            .ReturnsAsync(expectedId);

        // Act
        var result = await _sut.GetExistingDocumentIdAsync(hash);

        // Assert
        Assert.Equal(expectedId, result);
        _repositoryMock.Verify(r => r.GetExistingDocumentByHashAsync(hash), Times.Once);
    }


    [Fact]
    public async Task GetExistingDocumentIdAsync_ShouldReturnNull_WhenHashDoesNotExist()
    {
        // Arrange
        var hash = "NewHash";
        
        _repositoryMock
            .Setup(r => r.GetExistingDocumentByHashAsync(hash))
            .ReturnsAsync((int?)null);

        // Act
        var result = await _sut.GetExistingDocumentIdAsync(hash);

        // Assert
        Assert.Null(result);
    }


    [Theory]
    [InlineData("NULL_TEST")] 
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetExistingDocumentIdAsync_ShouldThrowArgumentException_WhenHashIsInvalid(string input)
    {
        var invalidHash = input.Equals("NULL_TEST") ? null : input;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GetExistingDocumentIdAsync(invalidHash!)
        );
    }


    [Fact]
    public async Task UpdateIndexCrawlTimeAsync_ShouldInvokeRepositoryWithCorrectParams()
    {
        // Arrange
        int docId = 42;
        var crawlTime = DateTime.UtcNow;

        // Act
        await _sut.UpdateIndexCrawlTimeAsync(docId, crawlTime);

        // Assert
        _repositoryMock.Verify(r => r.UpdateLastCrawledAsync(docId, crawlTime), Times.Once);
    }

}
