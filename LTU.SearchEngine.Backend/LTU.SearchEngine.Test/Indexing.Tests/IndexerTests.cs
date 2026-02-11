using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Infrastructure.Indexing.Repositories;
using Moq;
using System.Net;

namespace LTU.SearchEngine.Test.Indexing.IndexerTests
{
    public class IndexerTests
    {
        private readonly Mock<IIndexRepository> _repositoryMock;
        private readonly Mock<IndexingPipeline> _pipelineMock;

        private readonly Indexer _sut;

        public IndexerTests()
        {
            _repositoryMock = new Mock<IIndexRepository>();
            _pipelineMock = new Mock<IndexingPipeline>();

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
                timeTakenMs: 10
            );
        }

        [Fact]
        public void Index_ShouldThrow_WhenCrawlResultIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.Index(null!));
        }
        [Fact]
        public void Index_ShouldCallPipeline_WhenValidInput()
        {
            var crawlResult = CreateDummyCrawlResult();

            _pipelineMock
            .Setup(p => p.Transform(crawlResult))
            .Returns(new IndexDocument("1", "https://test.com"));

            // Act
            _sut.Index(crawlResult);

            // Assert
            _pipelineMock.Verify(p => p.Transform(crawlResult), Times.Once);

        }
        [Fact]
        public void Index_ShouldSaveTransformedDocument()
        {
            var crawlResult = CreateDummyCrawlResult();
            var indexDocument = new IndexDocument("1", "https://test.com");

            _pipelineMock
                .Setup(p => p.Transform(crawlResult))
                .Returns(indexDocument);

            // Act
            _sut.Index(crawlResult);

            // Assert
            _repositoryMock.Verify(r => r.Save(indexDocument), Times.Once);

        }
    }
}

