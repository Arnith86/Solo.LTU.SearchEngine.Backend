using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Crawling;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using System.Net;

namespace LTU.SearchEngine.Test.Crawling.Tests;

public class ProcessCrawlJobUseCaseTests
{
	private readonly Mock<ICrawler> _crawlerMock;
	private readonly Mock<IIndexer> _indexerMock;
	private readonly CrawlJob _crawlJob; 
	private readonly Mock<HttpMessageHandler> _handlerMock;

	private const string _c_ExpectedHash = "FakeHash";
	private const string _c_html = 
	"""
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

	private readonly ProcessCrawlJobUseCase _sut;

	public ProcessCrawlJobUseCaseTests()
	{
		_crawlerMock = new Mock<ICrawler>();
		_indexerMock = new Mock<IIndexer>();
		_handlerMock = new Mock<HttpMessageHandler>();

	
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
	public async Task Execute_ShouldReturnProcessJobResponse_WhenJobIsValidAndFetchSucceeds()
	{
		// Arrange 
		var indexedTerms = new List<IndexedTerm>
		{
			new IndexedTerm("engine", TermSource.Header),
			new IndexedTerm("indexing", TermSource.Body),
			new IndexedTerm("ranking", TermSource.Title)
		};

		var extractedLinks = new List<string>{"https://example.com/about"};
		var expectedContent = System.Text.Encoding.UTF8.GetBytes(_c_html);
		var fakeHash = "FakeHash";
		var processedAt = DateTime.UtcNow;

		var expectedResult = CrawlResultBuilder.BuildCrawlResult(
			url: _crawlJob.Url,
			title: "title",
			language: "sv",
			indexedTerms: indexedTerms,
			type: "text/html",
			content: _c_html,
			hashContent: fakeHash,
			extractedLinks: extractedLinks,
			statusCode: HttpStatusCode.OK,
			timeTakenMs: 342
		);

		var rawCrawlData = RawCrawlDataBuilder.BuildRawCrawlData(
			url: _crawlJob.Url,
			content: expectedContent,
			timeTaken: 342
		);

		var expectedResponse = CrawlResultBuilder.ProcessJobResponseBuilder(
			changedContent: true, 
			processedAt: processedAt,
			crawlResult: expectedResult
		);


		_crawlerMock.Setup(c => c.FetchRawAsync(_crawlJob.Url)).ReturnsAsync(rawCrawlData);
		_crawlerMock.Setup(c => c.GetContentHash(rawCrawlData)).ReturnsAsync(fakeHash);
		_crawlerMock.Setup(c => c.FetchAsync(rawCrawlData, fakeHash)).ReturnsAsync(expectedResult);

		HttpResponseHelper.SetupHttpResponse(_handlerMock, HttpStatusCode.OK, _c_html);

		// Act 
		ProcessJobResponse response = await _sut.Execute(_crawlJob);
		
		// Assert
		// Also checks to make sure that the `Indexer`method `index`, was executed. 
		Assert.InRange<DateTime>(response.ProcessedAt, processedAt, processedAt+TimeSpan.FromMilliseconds(50));
		Assert.Equal(expectedResponse.ChangedContent, response.ChangedContent);
		Assert.Equal(expectedResult, expectedResponse.CrawlResult);
		_indexerMock.Verify(im => im.IndexAsync(expectedResult), Times.Once);
	}


	[Fact]
	public async Task Execute_ShouldReturnUnchangedResponse_WhenHashAlreadyExists()
	{
		// Arrange
		int existingId = 1; 
		var fakeHash = "FakeHash";
		var processedAt = DateTime.UtcNow;

		_crawlerMock.Setup(c => c.FetchRawAsync(It.IsAny<string>())).ReturnsAsync(It.IsAny<RawCrawlData>());
		_crawlerMock.Setup(c => c.GetContentHash(It.IsAny<RawCrawlData>())).ReturnsAsync(fakeHash);
		_indexerMock.Setup(i => i.GetExistingDocumentIdAsync(fakeHash)).ReturnsAsync(existingId);

		var expectedProcessJobResponse = CrawlResultBuilder.ProcessJobResponseBuilder(
			changedContent: false, 
			processedAt: It.IsAny<DateTime>(),
			crawlResult: null
		);

		// Act 
		
		ProcessJobResponse response = await _sut.Execute(_crawlJob);
		// Assert 

		Assert.False(response.ChangedContent);
		Assert.InRange(response.ProcessedAt, processedAt, processedAt+TimeSpan.FromMilliseconds(30));
		Assert.Null(response.CrawlResult);
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
		var rawData = RawCrawlDataBuilder.BuildRawCrawlData(_crawlJob.Url);
		var fakeHash = "SomeHash";

		_crawlerMock.Setup(c => c.FetchRawAsync(_crawlJob.Url)).ReturnsAsync(rawData);
		_crawlerMock.Setup(c => c.GetContentHash(rawData)).ReturnsAsync(fakeHash);
		_indexerMock.Setup(i => i.GetExistingDocumentIdAsync(fakeHash)).ReturnsAsync((int?)null);

		_crawlerMock
			.Setup(c => c.FetchAsync(rawData, fakeHash))!
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
