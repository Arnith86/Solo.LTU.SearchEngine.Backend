using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Crawling;
using Moq;
using System.Net;
using System.Text;

namespace LTU.SearchEngine.Test.Crawling.Tests;

public class ProcessCrawlJobUseCaseTests
{
	private readonly Mock<ICrawler> _crawlerMock;
	private readonly Mock<IDomainValidator> _domainValidatorMock;
	private readonly Mock<IIndexer> _indexerMock;
	private readonly CrawlJob _crawlJob; 

	private readonly ProcessCrawlJobUseCase _sut;

	public ProcessCrawlJobUseCaseTests()
	{
		_crawlerMock = new Mock<ICrawler>();
		_domainValidatorMock
			 = new Mock<IDomainValidator>();
		_indexerMock = new Mock<IIndexer>();
		
		_crawlJob = new CrawlJob
		{
			Url = "http://www.testsite.com",
			Id = 1,
			Status = CrawlJobStatus.Pending
		};
		
		_sut = new ProcessCrawlJobUseCase(
			crawler: _crawlerMock.Object,
			domainValidator: _domainValidatorMock.Object,
			indexer: _indexerMock.Object);
	}


	[Fact]
	public async Task Execute_ShouldReturnCrawlResult_WhenJobIsValidAndFetchSucceeds()
	{
		// Arrange 
		var indexedTerms = new List<IndexedTerm>
		{
			new IndexedTerm("engine", TermSource.Header),
			new IndexedTerm("indexing", TermSource.Body),
			new IndexedTerm("ranking", TermSource.Title)
		};

		var html = """
			<!DOCTYPE html>
			<html>
			<head>
				<title>How Search Engines Work</title>
			</head>
			<body>
				<h1>Search Engines</h1>
				<p>Search engines use crawlers, indexers, and ranking algorithms.</p>
			</body>
			</html>
		""";

		var contentBytes = Encoding.UTF8.GetBytes(html);

		var extractedLinks = new List<string>
		{
			"https://example.com/about",
		};

		var expectedResult = new CrawlResult(
			url: _crawlJob.Url,
			title: "title",
			language: "sv",
			indexedTerms: indexedTerms,
			type: "text/html",
			content: contentBytes,
			extractedLinks: extractedLinks,
			statusCode: HttpStatusCode.OK,
			timeTakenMs: 342
		);

		_crawlerMock
			.Setup(c => c.FetchAsync(_crawlJob.Url))
			.ReturnsAsync(expectedResult);

		_domainValidatorMock.
			Setup(dv => dv.IsWhitelisted(_crawlJob.Url)).
			Returns(true);


		// Act 
		CrawlResult result = await _sut.Execute(_crawlJob);

		// Assert
		// Also checks to make sure that the `Indexer`method `index`, was executed. 
		Assert.Equal(expectedResult, result);
		_indexerMock.Verify(im => im.Index(expectedResult), Times.Once);
	}
}
