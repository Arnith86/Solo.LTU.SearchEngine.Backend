using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure.Configuration;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace LTU.SearchEngine.Test.BackgroundServices.Tests;

public class TplCrawlJobDispatcherTests
{
	private readonly Mock<ICrawlerSettingsLoader> _mockCrawlerSettingsLoader;
	private readonly SemaphoreProvider _semaphoreProvider;
	private Mock<IProcessCrawlJobUseCase> _mockUseCase;
	private readonly ICrawlJobDispatcher _sut;
	private readonly Mock<ILogger<TplCrawlJobDispatcher>> _mockLogger;

    private ProcessJobResponse CreateResponse()
	{
		var result = CrawlResultBuilder.BuildCrawlResult();
		return CrawlResultBuilder.ProcessJobResponseBuilder(
			changedContent: true,
			processedAt: DateTime.UtcNow,
			crawlResult: result
		);
	}
	
	private static CrawlerSettings CreateSettings()
	{
		return CrawlerSettingsBuilder.BuildCrawlerSettings(
			userAgent: "test-agent",
			maxConcurrencyPerDomain: 2,
			minDelayMs: 100,
			retryIntervals: new List<TimeSpan>
			{
				TimeSpan.FromMilliseconds(50),
				TimeSpan.FromMilliseconds(150),
				TimeSpan.FromMilliseconds(200)
			},
			crawlUpdateInterval: TimeSpan.FromMilliseconds(500),
            seedUrls: new List<string> { "ltu.se" },
			whiteList: new List<string> { "ltu.se" },
			robotsExceptionRules: new Dictionary<string, List<string>>{
                { "ltu.se", new List<string> { "/private/" } }
            }
		);
	}

	public TplCrawlJobDispatcherTests()
	{
		_semaphoreProvider = new SemaphoreProvider();
		_mockUseCase = new Mock<IProcessCrawlJobUseCase>();
		_mockUseCase.Setup(uc => uc.Execute(It.IsAny<CrawlJob>())).ReturnsAsync(CreateResponse());
		
		_mockCrawlerSettingsLoader = new Mock<ICrawlerSettingsLoader>();
		_mockCrawlerSettingsLoader.Setup(csl => csl.Load()).Returns(CreateSettings());
		_mockLogger = new Mock<ILogger<TplCrawlJobDispatcher>>();

        _sut = new TplCrawlJobDispatcher(
		  _mockUseCase.Object,
		  _semaphoreProvider,
		  _mockCrawlerSettingsLoader!.Object,
		  _mockLogger.Object
		);
    }

	[Fact]
	public void Constructor_IProcessCrawlJobUseCase_Null_ShouldThrow_ArgumentNullException()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() => new TplCrawlJobDispatcher(
			processCrawlJobUseCase: null!,
			crawlerSettingsLoader: _mockCrawlerSettingsLoader.Object,
			semaphoreProvider: _semaphoreProvider,
			logger: new Mock<ILogger<TplCrawlJobDispatcher>>().Object)
		);
	}

	[Fact]
	public void Constructor_CrawlerSetting_Null_ShouldThrow_ArgumentNullException()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() => new TplCrawlJobDispatcher(
			processCrawlJobUseCase: _mockUseCase.Object,
			crawlerSettingsLoader: null!,
			semaphoreProvider: _semaphoreProvider,
			logger: new Mock<ILogger<TplCrawlJobDispatcher>>().Object
            )
		);
	}
	
	[Fact]
	public void Constructor_SemaphoreProvider_Null_ShouldThrow_ArgumentNullException()
	{
		// Assert
		Assert.Throws<ArgumentNullException>(() => new TplCrawlJobDispatcher(
			processCrawlJobUseCase: _mockUseCase.Object,
			crawlerSettingsLoader: _mockCrawlerSettingsLoader.Object,
			semaphoreProvider: null!,
			logger: new Mock<ILogger<TplCrawlJobDispatcher>>().Object
			)
		);
	}

	[Fact]
	public async Task Enqueue_ImmediateJob_IsProcessed()
	{
		// Arrange
		var tcs = new TaskCompletionSource<bool>();
		using var cts = new CancellationTokenSource();

		var job = new CrawlJob
		{
			Id = 1,
			Url = "https://example.com",
			NextAttempt = DateTime.UtcNow
		};

		_mockUseCase.Setup(u => u.Execute(It.IsAny<CrawlJob>()))
			.ReturnsAsync(CreateResponse())
			.Callback(() => tcs.SetResult(true)
		);

		// Act
		var startTask = _sut.Start(cts.Token);
		await _sut.Enqueue(job);

		var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));

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
				return CreateResponse();
			}
		);

        var job = new CrawlJob
        {
            Id = 2,
            Url = "https://example.com",
            NextAttempt = DateTime.UtcNow.AddSeconds(1) 
        };

        using var cts = new CancellationTokenSource();
        var startTask = _sut.Start(cts.Token);

		// Act
        await _sut.Enqueue(job);
		
        // Assert (before scheduled time)
        await Task.Delay(200);
        // Verify that the job has not been executed yet
        Assert.False(executionSignal.Task.IsCompleted, "Job executed early!");

        // Assert (after scheduled time)
        await Task.Delay(1000);
        // Wait until the mocked Execute method signals execution
        await executionSignal.Task.WaitAsync(TimeSpan.FromSeconds(2));

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
				
				return CreateResponse();
			}
		);

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
	public async Task HandleUseCaseAsync_SuccessfulCrawl_JobReAddedToQueueWithCorrectNextAttempt()
	{
		// Arrange 
		var job = new CrawlJob
		{
			Id = 1,
			Url = "https://example.com",
			NextAttempt = DateTime.UtcNow
		};

		List<DateTime>  dateTimes = new List<DateTime>();

		_mockUseCase.Setup(uc => uc.Execute(It.IsAny<CrawlJob>())).Returns( async () =>
			{
				var result = CreateResponse();
				dateTimes.Add(DateTime.UtcNow);
				return result;
			}
		);
		
		// Act
		var cts = new CancellationTokenSource();
		
		await _sut.Enqueue(job);
		Task task = _sut.Start(cts.Token);

		int totalWait = 0;
		int maxWait = 5000;
		
		while (totalWait < maxWait && dateTimes.Count < 2)
		{
			totalWait += 100;
			await Task.Delay(100);
		}

		cts.Cancel();

		// Assert - CrawlUpdateInterval = 500
		TimeSpan elapsedTime = dateTimes[1] - dateTimes[0];
		Assert.InRange(elapsedTime, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(560));
		_mockUseCase.Verify(uc => uc.Execute(It.IsAny<CrawlJob>()), Times.Exactly(2));
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
		// Arrange
		DateTime nextAttemptExpected = 
			DateTime.UtcNow + _mockCrawlerSettingsLoader.Object.Load().GetRetryDelayInterval(1);
		
		DateTime nextAttemptToLong 
			= DateTime.UtcNow +  _mockCrawlerSettingsLoader.Object.Load().GetRetryDelayInterval(2); 


		var job = new CrawlJob
		{
			Id = 3,
			Url = "https://fail.com",
			RetryCount = 1,
			NextAttempt = DateTime.UtcNow
		};

		_mockUseCase.SetupSequence(u => u.Execute(It.IsAny<CrawlJob>()))
			.ThrowsAsync(new InvalidOperationException("fetch failed"))
			.ReturnsAsync(CreateResponse()
		);

		using var cts = new CancellationTokenSource();
		
		var startTask = _sut.Start(cts.Token);

		// Act
		await _sut.Enqueue(job);
		
		int maxWait = 500;
		int totWait = 0;
		while ((job.RetryCount < 2) && totWait < maxWait)
		{
			await Task.Delay(100);
			totWait += 100;
		}
		
		// Assert
		Assert.Equal(2, job.RetryCount);
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
			.ReturnsAsync(CreateResponse()
		);

		using var cts = new CancellationTokenSource();
		var startTask = _sut.Start(cts.Token);

		// Act
		await _sut.Enqueue(job);

		await Task.Delay(500);

		// Assert - Retry counter is only incremented before enqueue.
		Assert.Equal(3, job.RetryCount);
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Discarding job")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()!
			),
			Times.Once
		);

		cts.Cancel();
		await startTask;
	}

	[Fact]
	public async Task ExtractedLinks_CreateNewJobs()
	{
		var result = CrawlResultBuilder.BuildCrawlResult(
			url: "https://root.com",
			title: "root",
			language: "en",
			type: "text/html",
			indexedTerms: new List<IndexedTerm>(),
			content: "x",
			extractedLinks: new List<string> { "https://a.com", "https://b.com" },
			statusCode: HttpStatusCode.OK,
			timeTakenMs: 10
		);

		var response = CrawlResultBuilder.ProcessJobResponseBuilder(
			changedContent: true, 
			processedAt: DateTime.UtcNow, 
			crawlResult: result
		);

		_mockUseCase.Setup(u => u.Execute(It.IsAny<CrawlJob>()))
			.ReturnsAsync(response);

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
				
				await Task.Delay(200); // simulate work
				Interlocked.Decrement(ref active);

				return CreateResponse();
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

		Assert.Equal( 
			_mockCrawlerSettingsLoader.Object.Load().MaxConcurrencyPerDomain, 
			maxObserved
		);

		cts.Cancel();
		await taskStart;
	}

}
