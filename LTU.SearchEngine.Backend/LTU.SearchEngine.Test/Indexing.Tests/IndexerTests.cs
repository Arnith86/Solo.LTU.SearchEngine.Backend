using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Infrastructure.Repositories;
using Moq;
using System.Net;
using System.Threading.Tasks; // Behövs för Task
using Xunit;
using IIndexingPipeline = LTU.SearchEngine.Backend.Core.Model.IIndexingPipeline; // Antar att du använder Xunit baserat på [Fact]

namespace LTU.SearchEngine.Test.Indexing.IndexerTests
{
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
            return new CrawlResult(
                url: "https://test.com",
                title: "Test",
                language: "en",
                indexedTerms: new List<IndexedTerm>
                {
                    new IndexedTerm("engine", TermSource.Title)
                },
                type: "text/html",
                content: new byte[0],
                extractedLinks: new List<string>(),
                statusCode: HttpStatusCode.OK,
                timeTakenMs: 10,
                "FakeHash"
            );
        }

        [Fact]
        public async Task IndexAsync_ShouldThrow_WhenCrawlResultIsNull()
        {
            // Act & Assert
            // Vi måste använda ThrowsAsync när metoden returnerar en Task
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.IndexAsync(null!));
        }

        [Fact]
        public async Task IndexAsync_ShouldCallPipeline_WhenValidInput()
        {
            var crawlResult = CreateDummyCrawlResult();

            // Lade till "Test" som titel i IndexDocument-konstruktorn
            _pipelineMock
                .Setup(p => p.Transform(crawlResult))
                .Returns(new IndexDocument("1", "https://test.com", "Test"));

            // Act
            // Vi måste ha await framför asynkrona metodanrop i tester
            await _sut.IndexAsync(crawlResult);

            // Assert
            _pipelineMock.Verify(p => p.Transform(crawlResult), Times.Once);
        }

        [Fact]
        public async Task IndexAsync_ShouldSaveTransformedDocument()
        {
            var crawlResult = CreateDummyCrawlResult();

            // Lade till "Test" som titel i IndexDocument-konstruktorn
            var indexDocument = new IndexDocument("1", "https://test.com", "Test");

            _pipelineMock
                .Setup(p => p.Transform(crawlResult))
            .Returns(indexDocument); 

            // Act
            await _sut.IndexAsync(crawlResult); 

            // Assert
            _repositoryMock.Verify(r => r.SaveAsync(indexDocument), Times.Once);
        }
    }
    
}