using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Configurations;
using LTU.SearchEngine.Infrastructure.Crawling;
using LTU.SearchEngine.Test.HelperClasses;
using Moq;
using System.Net;
using System.Text;

namespace LTU.SearchEngine.Test.Crawling.Tests;

public class ProcessCrawlJobUseCaseTests
{
	private readonly Mock<ICrawler> _crawlerMock;
	private readonly Mock<IIndexer> _indexerMock;
	private readonly CrawlJob _crawlJob; 

	private readonly ProcessCrawlJobUseCase _sut;

	public ProcessCrawlJobUseCaseTests()
	{
		_crawlerMock = new Mock<ICrawler>();
		_indexerMock = new Mock<IIndexer>();
		
		_crawlJob = new CrawlJob
		{
			Url = "http://www.testsite.com",
			Id = 1,
			Status = CrawlJobStatus.Pending
		};
		
		_sut = new ProcessCrawlJobUseCase(
			_crawlerMock.Object,
			_indexerMock.Object
		);
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

		var extractedLinks = new List<string>
		{
			"https://example.com/about",
		};

		var expectedResult = CrawlResultBuilder.BuildCrawlResult(
			url: _crawlJob.Url,
			title: "title",
			language: "sv",
			indexedTerms: indexedTerms,
			type: "text/html",
			content: html,
			extractedLinks: extractedLinks,
			statusCode: HttpStatusCode.OK,
			timeTakenMs: 342
		);

		
		_crawlerMock
			.Setup(c => c.FetchAsync(_crawlJob.Url))
			.ReturnsAsync(expectedResult);

		
		// Act 
		CrawlResult result = await _sut.Execute(_crawlJob);

		// Assert
		// Also checks to make sure that the `Indexer`method `index`, was executed. 
		Assert.Equal(expectedResult, result);
		_indexerMock.Verify(im => im.IndexAsync(expectedResult), Times.Once);
	}

	[Fact]
	public async Task Execute_ShouldThrowArgumentNullException_WhenJobIsNull()
	{
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.Execute(null!));
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("NULL_TEST")]
	public async Task Execute_ShouldThrowArgumentException_WhenUrlIsEmpty(string input)
	{
		string? url = input.Equals("NULL_TEST") ? null : input;

		// Arrange
		_crawlJob.Url = url!;

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(() => _sut.Execute(_crawlJob));
	}


	[Fact]
	public async Task Execute_ShouldThrowInvalidOperationException_WhenCrawlerReturnsNull()
	{
		// Arrange
		_crawlerMock
			.Setup(c => c.FetchAsync(_crawlJob.Url))!
			.ReturnsAsync((CrawlResult?)null);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(_crawlJob));
		Assert.Contains("Failed to fetch URL", ex.Message);
		Assert.Contains(_crawlJob.Url, ex.Message);
	}


	[Fact]
	public void Constructor_ShouldThrow_WhenCrawlerIsNull()
	{
		// Assert 
		Assert.Throws<ArgumentNullException>(() =>
			new ProcessCrawlJobUseCase(
				null!, 
				_indexerMock.Object
			)
		);
	}

	[Fact]
	public void Constructor_ShouldThrow_WhenDomainValidatorIsNull()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() =>
			new ProcessCrawlJobUseCase(
				_crawlerMock.Object, 
				null!
			)
		);
	}


	[Fact]
	public void Constructor_ShouldThrow_WhenIndexerIsNull()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() =>
			new ProcessCrawlJobUseCase(
				_crawlerMock.Object, 
				null!
			)
		);
	}
}
