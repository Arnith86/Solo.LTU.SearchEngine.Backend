using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.BackgroundServices;
using Moq;
using System.Net;
using System.Text;

namespace LTU.SearchEngine.Test.BackgroundServices.Tests;

public class TplCrawlJobDispatcherTests
{
	private readonly CrawlerSettings _crawlerSettings;
	private readonly SemaphoreProvider _semaphoreProvider;
	private Mock<IProcessCrawlJobUseCase> _mockUseCase;
	private readonly ICrawlJobDispatcher _sut;

	private CrawlResult CreateResult()
	{
		return new CrawlResult(
			url: "https://example.com",
			title: "x",
			language: "en",
			indexedTerms: new List<IndexedTerm>(),
			type: "text/html",
			content: Encoding.UTF8.GetBytes("x"),
			extractedLinks: Array.Empty<string>(),
			statusCode: HttpStatusCode.OK,
			timeTakenMs: 1
		);
	}
	
	private static CrawlerSettings CreateSettings()
	{
		return new CrawlerSettings(
			userAgent: "test-agent",
			maxConcurrencyPerDomain: 2,
			minDelayMs: 0,
			retryIntervals: new List<TimeSpan>
			{
			TimeSpan.FromMilliseconds(50),
			TimeSpan.FromMilliseconds(100),
			TimeSpan.FromMilliseconds(200)
			}
		);
	}

	public TplCrawlJobDispatcherTests()
	{
		_crawlerSettings = CreateSettings();
		_semaphoreProvider = new SemaphoreProvider();
		_mockUseCase = new Mock<IProcessCrawlJobUseCase>();
		_sut = new TplCrawlJobDispatcher(
			processCrawlJobUseCase: _mockUseCase.Object,
			crawlerSettings: _crawlerSettings,
			semaphoreProvider: _semaphoreProvider
		);
	}

	[Fact]
	public void Constructor_IProcessCrawlJobUseCase_Null_ShouldThrow_ArgumentNullException()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() => new TplCrawlJobDispatcher(
			processCrawlJobUseCase: null!,
			crawlerSettings: _crawlerSettings,
			semaphoreProvider: _semaphoreProvider)
		);
	}

	[Fact]
	public void Constructor_CrawlerSetting_Null_ShouldThrow_ArgumentNullException()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() => new TplCrawlJobDispatcher(
			processCrawlJobUseCase: _mockUseCase.Object,
			crawlerSettings: null!,
			semaphoreProvider: _semaphoreProvider)
		);
	}
	
	[Fact]
	public void Constructor_SemaphoreProvider_Null_ShouldThrow_ArgumentNullException()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() => new TplCrawlJobDispatcher(
			processCrawlJobUseCase: _mockUseCase.Object,
			crawlerSettings: _crawlerSettings,
			semaphoreProvider: null!)
		);
	}

}
