using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Exceptions;
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
			minDelayMs: 100,
			retryIntervals: new List<TimeSpan>
			{
			TimeSpan.FromMilliseconds(50),
			TimeSpan.FromMilliseconds(150),
			TimeSpan.FromMilliseconds(200)
			},
            seedUrls: new List<string> { "ltu.se" }
        );
	}

	public TplCrawlJobDispatcherTests()
	{
		_crawlerSettings = CreateSettings();
		_semaphoreProvider = new SemaphoreProvider();
		_mockUseCase = new Mock<IProcessCrawlJobUseCase>();

        _sut = new TplCrawlJobDispatcher(
		  _mockUseCase.Object,
		  _crawlerSettings,
		  _semaphoreProvider
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
			semaphoreProvider: _semaphoreProvider
            )
		);
	}
	
	[Fact]
	public void Constructor_SemaphoreProvider_Null_ShouldThrow_ArgumentNullException()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() => new TplCrawlJobDispatcher(
			processCrawlJobUseCase: _mockUseCase.Object,
			crawlerSettings: _crawlerSettings,
			semaphoreProvider: null!
			)
		);
	}

	[Fact]
	public async Task Enqueue_ImmediateJob_IsProcessed()
	{
		// Arrange
		var job = new CrawlJob
		{
			Id = 1,
			Url = "https://example.com",
			NextAttempt = DateTime.UtcNow
		};

		_mockUseCase.Setup(u => u.Execute(It.IsAny<CrawlJob>()))
			.ReturnsAsync(CreateResult());

		using var cts = new CancellationTokenSource();

		// Act
		var startTask = _sut.Start(cts.Token);
		await _sut.Enqueue(job);

		await Task.Delay(300);
		cts.Cancel();
		await startTask;

		// Assert
		// Verify that the use case was called with the right job.
		_mockUseCase.Verify(u => u.Execute(
			It.Is<CrawlJob>(j => j.Id == 1)), 
			Times.Once
		);
	}

    [Fact]
    public async Task Enqueue_ScheduledJob_IsNotProcessedBeforeNextAttempt()
    {
       // Used to signal when the mocked Execute method is actually invoked.   
        var executionSignal = new TaskCompletionSource<bool>();

        // Signal when Execute is invoked to wait deterministically in the test.
        _mockUseCase.Setup(u => u.Execute(It.IsAny<CrawlJob>()))
		.Returns(async () =>
		{
			executionSignal.TrySetResult(true);
			return CreateResult();
		});

        var job = new CrawlJob
        {
            Id = 2,
            Url = "https://example.com",
            NextAttempt = DateTime.UtcNow.AddMilliseconds(300) 
        };

        using var cts = new CancellationTokenSource();
        var startTask = _sut.Start(cts.Token);

		// Act
        await _sut.Enqueue(job);

        // Assert (before scheduled time)
        await Task.Delay(100);
        // Verify that the job has not been executed yet
        Assert.False(executionSignal.Task.IsCompleted);

        // Assert (after scheduled time)
        await Task.Delay(300);
        // Wait until the mocked Execute method signals execution
        await executionSignal.Task.WaitAsync(TimeSpan.FromSeconds(1));

        _mockUseCase.Verify(u => u.Execute(It.IsAny<CrawlJob>()), Times.Once);

        cts.Cancel();
        await startTask;
    }
    
    [Fact]
	public async Task Enqueue_ScheduledJob_JobWithNearestNextAttempt_ExecuteFirst()
	{
		List<int> executionOrder = new List<int>();
		var gate = new SemaphoreSlim(0, 10);
		var now = DateTime.UtcNow;

		var job1 = new CrawlJob
		{
			Id = 1,
			Url = "https://example.com",
			NextAttempt = DateTime.UtcNow.AddMilliseconds(400)
		};

		var job2 = new CrawlJob
		{
			Id = 2,
			Url = "https://example.com",
			NextAttempt = DateTime.UtcNow.AddMilliseconds(100)
		};

		// Each time the method Execute is called, we add the job id to the
		// executionOrder list to be able to assert the order of execution. 
		_mockUseCase.Setup(u => u.Execute(It.IsAny<CrawlJob>()))
			.Returns( async (CrawlJob j) => 
			{
				lock (executionOrder) executionOrder.Add(j.Id);
				gate.Release();
				
				return CreateResult();
			});

		using var cts = new CancellationTokenSource();
		var startTask = _sut.Start(cts.Token);

		// Act
		await _sut.Enqueue(job1);
		await _sut.Enqueue(job2);

		// Wait for both job to be executed.
		await gate.WaitAsync(TimeSpan.FromSeconds(2));
		await gate.WaitAsync(TimeSpan.FromSeconds(2));

		// Assert - job2 should execute before job1 because of earlier NextAttempt.
		Assert.Equal(2, executionOrder.Count);
		Assert.Equal(2, executionOrder[0]);
		Assert.Equal(1, executionOrder[1]);

		cts.Cancel();
		await startTask;
	}

	[Fact]
	public async Task Enqueue_Job_Null_ShouldThrow_ArgumentNullException()
	{
		CrawlJob job = null!;
		// Act + Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.Enqueue(job));
	}

	[Fact]
	public async Task HandleFailedJob_MaxRetryNotReached_IsRetriedWithCorrectTimeDelayAdded()
	{
		DateTime nextAttemptExpected = DateTime.UtcNow + _crawlerSettings.RetryIntervals[0];
		DateTime nextAttemptToLong = DateTime.UtcNow + _crawlerSettings.RetryIntervals[1];

		var job = new CrawlJob
		{
			Id = 3,
			Url = "https://fail.com",
			RetryCount = 0,
			NextAttempt = DateTime.UtcNow
		};

		_mockUseCase.SetupSequence(u => u.Execute(It.IsAny<CrawlJob>()))
			.ThrowsAsync(new InvalidOperationException("fetch failed"))
			.ReturnsAsync(CreateResult()
		);

		using var cts = new CancellationTokenSource();
		var startTask = _sut.Start(cts.Token);

		// Act
		await _sut.Enqueue(job);

		await Task.Delay(500);

		// Assert
		_mockUseCase.Verify(u => u.Execute(It.IsAny<CrawlJob>()), Times.AtLeast(2));
		Assert.Equal(1, job.RetryCount);
		Assert.True(
			job.NextAttempt >= nextAttemptExpected && 
			job.NextAttempt <= nextAttemptToLong
		);

		cts.Cancel();
		await startTask;
	}

	[Fact]
	public async Task HandleFailedJob_MaxRetryReached_JobIsDropped()
	{
		var job = new CrawlJob
		{
			Id = 2,
			Url = "https://fail.com",
			RetryCount = 3,
			NextAttempt = DateTime.UtcNow
		};

		_mockUseCase.SetupSequence(u => u.Execute(It.IsAny<CrawlJob>()))
			.ThrowsAsync(new InvalidOperationException("fetch failed"))
			.ReturnsAsync(CreateResult()
		);

		using var cts = new CancellationTokenSource();
		var startTask = _sut.Start(cts.Token);

		// Act
		await _sut.Enqueue(job);

		await Task.Delay(500);

		// Assert - Retry counter is only incremented before enqueue.
		Assert.Equal(3, job.RetryCount);

		cts.Cancel();
		await startTask;
	}

	[Fact]
	public async Task DomainNotWhitelisted_DoesNotRetry()
	{
		var job = new CrawlJob
		{
			Id = 4,
			Url = "https://blocked.com",
			NextAttempt = DateTime.UtcNow
		};

		_mockUseCase.Setup(u => u.Execute(It.IsAny<CrawlJob>()))
			.ThrowsAsync(new DomainNotWhitelistedException(job.Url));

		using var cts = new CancellationTokenSource();
		var startTask = _sut.Start(cts.Token);

		// Act
		await _sut.Enqueue(job);

		await Task.Delay(300);

		// Assert
		_mockUseCase.Verify(u => u.Execute(It.IsAny<CrawlJob>()), Times.Once);

		cts.Cancel();
		await startTask;
	}

	[Fact]
	public async Task ExtractedLinks_CreateNewJobs()
	{
		var result = new CrawlResult(
			url: "https://root.com",
			title: "root",
			language: "en",
			indexedTerms: new List<IndexedTerm>(),
			type: "text/html",
			content: Encoding.UTF8.GetBytes("x"),
			extractedLinks: new[] { "https://a.com", "https://b.com" },
			statusCode: HttpStatusCode.OK,
			timeTakenMs: 10
		);

		_mockUseCase.Setup(u => u.Execute(It.IsAny<CrawlJob>()))
			.ReturnsAsync(result);

		using var cts = new CancellationTokenSource();
		var taskStart = _sut.Start(cts.Token);

		await _sut.Enqueue(new CrawlJob
		{
			Id = 10,
			Url = "https://root.com",
			NextAttempt = DateTime.UtcNow
		});

		await Task.Delay(500);

		// root + 2 extracted links
		_mockUseCase.Verify(u => u.Execute(It.IsAny<CrawlJob>()), Times.AtLeast(3));

		cts.Cancel();
		await taskStart;
	}

	[Fact]
	public async Task MaxConcurrencyPerDomain_IsRespected()
	{
		int active = 0;
		int maxObserved = 0;

		_mockUseCase.Setup(u => u.Execute(It.IsAny<CrawlJob>()))
			.Returns(async () =>
			{
				var current = Interlocked.Increment(ref active);
				maxObserved = Math.Max(maxObserved, current);
				
				await Task.Delay(100); // simulate work
				Interlocked.Decrement(ref active);

				return CreateResult();
			});

		
		using var cts = new CancellationTokenSource();
		var taskStart = _sut.Start(cts.Token);

		for (int i = 0; i < 10; i++)
		{
			await _sut.Enqueue(new CrawlJob
			{
				Id = i,
				Url = "https://same-domain.com",
				NextAttempt = DateTime.UtcNow
			});
		}

		await Task.Delay(2000);

		Assert.Equal(_crawlerSettings.MaxConcurrencyPerDomain, maxObserved);

		cts.Cancel();
		await taskStart;
	}

}
