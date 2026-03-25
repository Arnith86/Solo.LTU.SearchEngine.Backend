using LTU.SearchEngine.Backend.Api;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure.Configurations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using LTU.SearchEngine.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using LTU.SearchEngine.Test.HelperClasses;

namespace LTU.SearchEngine.Test.IntegrationTesting;


public class CrawlerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory; 
    private readonly HelperClasses.WebHostBuilder _webHostBuilder;
    private string _tempSettingsPath; 
     
    public CrawlerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _webHostBuilder = new HelperClasses.WebHostBuilder();
         _tempSettingsPath = 
            Path.Combine(Path.GetTempPath(), $"Settings_{Guid.NewGuid()}.ToJsonSchemaType");
    } 

    private WebApplicationFactory<Program> CreateTestFactory<T>(
        Mock<IIndexer> indexerMock,
        HttpClient httpClientForCrawler,
        Mock<ILogger<T>>? logger = null,
        string seedUrl = "http://localhost/page1.html",
        int maxConcurrencyPerDomain = 2,
        int minDelayMs = 10,
        Mock<IRobotsHandler>? robotsHandlerMock = null
    ) where T : class
    {
        string fakeAppSettings = $$$"""
        {
            "CrawlerSettings": {
                "UserAgent": "TestBot",
                "MaxConcurrencyPerDomain": {{{maxConcurrencyPerDomain}}},
                "MinDelayMs": {{{minDelayMs}}},
                "RetryIntervals": ["00:00:01"],
                "SeedUrls": ["{{{seedUrl}}}"],
                "WhiteList": ["localhost"],
                "RobotsExceptionRules": {
                    "localhost": ["/ignoreThisRule/ignored.html"], 
                    "anotherDomain.com": ["/secret/"]
                }
            }
        }
        """;

        File.WriteAllText(_tempSettingsPath, fakeAppSettings);

        // Creates a version of the application, but with a mock index instead.
        var testFactory = _factory.WithWebHostBuilder(builder =>
        {
            
            // Replaces the preconfigured seed url with the test version. 
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile(_tempSettingsPath, optional: false, reloadOnChange: true);
            }); 

            builder.ConfigureServices(services =>
            {
                // This finds the service that we want to remove from the application startup
                var descriptor = services.FirstOrDefault(d => 
                    d.ImplementationType == typeof(CrawlBackgroundService));

                if (descriptor is not null) services.Remove(descriptor);

                services.AddSingleton(httpClientForCrawler); // Replace the HttpClient the client uses to the in-memory webHostBuilder
                services.AddSingleton(indexerMock.Object);
                if (logger is not null) services.AddSingleton(logger.Object);
                if (robotsHandlerMock is not null) services.AddSingleton(robotsHandlerMock.Object);
            });

        });

        return testFactory;
    } 


    [Fact]
    [Trait("TestCase", "TC-FRQ-1001")]
    public async Task Crawler_ShouldVisitAllLinkedPagesRecursively()
    {
        // Arrange 
        string seedURL = "http://localhost/seed.html";
        string page1 = "http://localhost/page1.html";
        string page2 = "http://localhost/page2.html";
        string final = "http://localhost/final.html";
    
        CrawlJob crawlJob = new CrawlJob{Url = seedURL, NextAttempt = DateTime.UtcNow};

        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        var webHelper = new HelperClasses.WebHostBuilder();
        using var httpClientForCrawler = webHelper.BuildHttpClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory<TplCrawlJobDispatcher>(
            indexerMock: indexerMock, 
            httpClientForCrawler : httpClientForCrawler
        );


        // Retrieve the actual implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
       
        // Act
        var cts = new CancellationTokenSource();

        await dispatcher.Enqueue(crawlJob);
        
        try
        {
            _ = dispatcher.Start(cts.Token);
        
            int timeoutMs = 5000;
            int elapsedTime = 0;

            // Wait up to 5 sec to fill list 
            while (visitedList.Count < 4 && elapsedTime < timeoutMs)
            {
                await Task.Delay(100); // wait with 100 ms intervals
                elapsedTime += 100;
            }
        }
        finally
        {
            cts.Cancel();
        }
      
        // Assert
        Assert.Equal(4, visitedList.Count);
        Assert.Equal(seedURL, visitedList[0].Url);
        Assert.Equal(page1, visitedList[1].Url);
        Assert.Equal(page2, visitedList[2].Url);
        Assert.Equal(final, visitedList[3].Url);
    }


    [Fact]
    [Trait("TestCase", "TC-FRQ-1001-N1")]
    public async Task Crawler_ShouldIgnoreSeedUrl_WhenDomainIsNotWhitelisted()
    {
        // Arrange 
        string externalURL = "http://forbidden-domain.com";
        CrawlJob crawlJob = new CrawlJob{ Url = externalURL, NextAttempt = DateTime.UtcNow};

        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        var webHelper = new HelperClasses.WebHostBuilder();
        using var httpClientForCrawler = webHelper.BuildHttpClient();
        
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory<TplCrawlJobDispatcher>(
            indexerMock: indexerMock, 
            httpClientForCrawler : httpClientForCrawler,
            seedUrl: externalURL
        );
        

        // Retrieve the actually implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();

        // Act
        var cts = new CancellationTokenSource();
        
        await dispatcher.Enqueue(crawlJob);
        
        try
        {
            _ = dispatcher.Start(cts.Token);    
            
            int timeoutMs = 500;
            int elapsedTime = 0;

            // Wait up to 5 sec to fill list 
            while (elapsedTime < timeoutMs)
            {
                await Task.Delay(100); // wait with 100 ms intervals
                elapsedTime += 100;
            }

        }
        finally
        {
            cts.Cancel();
        }
       

        // Assert
        Assert.Empty(visitedList);
        indexerMock.Verify(im => im.IndexAsync(
            It.IsAny<CrawlResult>()), 
            Times.Never()
        );
    }

    [Fact]
    [Trait("TestCase", "TC-FRQ-1003-B")]
    public async Task Crawler_WhitelistEnforcement_IgnoresNonWhiteListedDomains()
    {
        // Arrange 
        // includes domain external-domain.com
        string seedURL = "http://localhost/SeedIncludingExternalUrl.html"; 
        string page1 = "http://localhost/page1.html";
        string page2 = "http://localhost/page2.html";
        string final = "http://localhost/final.html";

        CrawlJob crawlJob = new CrawlJob{ Url = seedURL, NextAttempt = DateTime.UtcNow };

        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        using var httpClientForCrawler = _webHostBuilder.CreateFakeInternetClient();
        
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory<TplCrawlJobDispatcher>(
            indexerMock: indexerMock, 
            httpClientForCrawler : httpClientForCrawler,
            seedUrl: seedURL
        );
        
        // Retrieve the actually implementation of the CrawlJobDispatcher. 
        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();


        // Act
        await dispatcher.Enqueue(crawlJob);
        var cts = new CancellationTokenSource();
        
        try
        {
            _ = dispatcher.Start(cts.Token);
            
            int timeoutMs = 4000;
            int elapsedTime = 0;

            // Wait up to 4 sec to fill list 
            while (visitedList.Count < 4 && elapsedTime < timeoutMs)
            {
                await Task.Delay(100); // wait with 100 ms intervals
                elapsedTime += 100;
            }
        }
        finally
        {
            cts.Cancel();
        }

        // Assert
        Assert.Equal(4, visitedList.Count);
        Assert.Equal(seedURL, visitedList[0].Url);
        Assert.Equal(page1, visitedList[1].Url);
        Assert.Equal(page2, visitedList[2].Url);
        Assert.Equal(final, visitedList[3].Url);
    }

    
    [Fact]
    [Trait("TestCase", "TC-FRQ-1003-A")]
    public async Task Crawler_DomainRateAndConcurrencyLimitEnforcement()
    {
        // Arrange 
        // Setting up timing instruments
        string seedURL = "http://localhost/seed.html";
        int maxConcurrencyPerDomain = 2;
        int minDelayMs = 200;

        var timesStamps = new List<DateTime>();
        int activeRequests = 0;
        int maxObservedConcurrency = 0;
        var lockObject = new Object();

        var robotsHandlerMock = new Mock<IRobotsHandler>();
        robotsHandlerMock.Setup(rh => rh.IsAllowedAsync(It.IsAny<string>())).ReturnsAsync(true);


     
        using var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults( webBuilder =>
            {
                webBuilder.UseTestServer();
                
                webBuilder.Configure(app =>
                {
                   app.Run( async context =>
                   {
                       // log concurrency
                       var current = Interlocked.Increment(ref activeRequests);
                       lock (lockObject)
                       {
                           maxObservedConcurrency = Math.Max(maxObservedConcurrency, current); // Used to map concurrency.
                           timesStamps.Add(DateTime.UtcNow); // Used to map delay.
                       }

                       // we force the connection to stay open to measure concurrency 
                       await Task.Delay(300); 
                       Interlocked.Decrement(ref activeRequests);
                       await context.Response.WriteAsync("<html><body>Done</body></html>");
                   });
                });
            }).Build();; 
        
        await host.StartAsync();



        using WebApplicationFactory<Program> testFactory = CreateTestFactory<TplCrawlJobDispatcher>(
            indexerMock: new Mock<IIndexer>(), 
            httpClientForCrawler: host.GetTestClient(),
            seedUrl: seedURL,
            maxConcurrencyPerDomain: maxConcurrencyPerDomain,
            minDelayMs: minDelayMs,
            robotsHandlerMock: robotsHandlerMock
        );
        
        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
        var settingsLoader = scope.ServiceProvider.GetRequiredService<ICrawlerSettingsLoader>();       
        
        // Act
        using var cts = new CancellationTokenSource();
        var dispatcherTask = dispatcher.Start(cts.Token);

        for (int i = 0; i < 5; i++)
        {
            await dispatcher.Enqueue(new CrawlJob
            {
                Url = $"http://localhost/page{i}",
                NextAttempt = DateTime.UtcNow
            });
        }

        // Ensures that the dispatcher was given enough time to execute
        await Task.Delay(3000); 
        cts.Cancel();

        // Assert - Concurrency
        Assert.True(
            maxObservedConcurrency <= settingsLoader.Load().MaxConcurrencyPerDomain, 
            $"observed: {maxObservedConcurrency}, expected: {settingsLoader.Load().MaxConcurrencyPerDomain}"
        );

        // Assert - Delay
        // Makes sure that execution times are in sequential order.
        var orderedStamps = timesStamps.OrderBy(ts => ts); 

        for (int i = 0; i < timesStamps.Count - 1; i++)
        {   
            if (i + 1 <= timesStamps.Count)
            {
                var actualDelay = timesStamps[i + 1] - timesStamps[i];
                Assert.True(
                    actualDelay.TotalMilliseconds >= minDelayMs - 20, // Added small margin 
                    $"Delay between call {i} and {i+1} was to short: expected: {minDelayMs}ms actual: {actualDelay.TotalMilliseconds}ms"
                );     
            }
        }
    }


    [Fact]
    [Trait("TestCase", "TC-FRQ-1004")]
    public async Task Crawler_LoggingMaxConcurrencyPerDomainAndMinDelayMsAtStartup()
    {
        var cts = new CancellationTokenSource();    
        // Arrange 
        string ExpectedUserAgent = "TestBot";
        string ExpectedSeedURL = "http://localhost/page1.html"; 
        int ExpectedMaxConcurrencyPerDomain = 2;
        int ExpectedMinDelayMs = 10;
        string expectedRetryInterval = "00:00:01, 1.00:00:00, 7.00:00:00";
        string ExpectedWhiteList = "localhost";

        var indexerMock = new Mock<IIndexer>();
        var loggerMock = new Mock<ILogger<TplCrawlJobDispatcher>>();

        using var httpClientForCrawler = _webHostBuilder.CreateFakeInternetClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory<TplCrawlJobDispatcher>(
            logger: loggerMock,
            indexerMock: indexerMock, 
            httpClientForCrawler: httpClientForCrawler
        );

        // Retrieve the actually implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
        var settingsLoader = scope.ServiceProvider.GetRequiredService<ICrawlerSettingsLoader>();

        // Act  
        _ = dispatcher.Start(cts.Token);
        await Task.Delay(100);

        // Assert
        loggerMock.Verify(l => l.Log(
            logLevel: LogLevel.Information,
            eventId: It.IsAny<EventId>(),
            state: It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains($"UserAgent={ExpectedUserAgent}") && 
                v.ToString()!.Contains($"SeedURLs={ExpectedSeedURL}") && 
                v.ToString()!.Contains($"MaxConcurrencyPerDomain={ExpectedMaxConcurrencyPerDomain}") && 
                v.ToString()!.Contains($"MinDelayMs={ExpectedMinDelayMs}ms") && 
                v.ToString()!.Contains($"RetryIntervals={expectedRetryInterval}") && 
                v.ToString()!.Contains($"WhiteList={ExpectedWhiteList}")  
                ),  
                
            exception: null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once); 
    }    
   
   
    [Fact]
    [Trait("TestCase", "TC-FRQ-1005")]
    public async Task Crawler_CrawlerRobotsCompliance()
    {
        // Arrange 
        var cts = new CancellationTokenSource();    
        
        string seedURL = "http://localhost/robots-test-start.html"; 
        
        var indexerMock = new Mock<IIndexer>();
        var loggerMock = new Mock<ILogger<RobotsHandler>>();
        CallTracker callTracker = new CallTracker();

        using var httpClientForCrawler = _webHostBuilder.CreateFakeInternetClient(callTracker);
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory<RobotsHandler>(
            indexerMock: indexerMock, 
            httpClientForCrawler: httpClientForCrawler,
            loggerMock
        );

        // Retrieve the actually implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
        var settingsLoader = scope.ServiceProvider.GetRequiredService<ICrawlerSettingsLoader>();

        // Act  
        await dispatcher.Enqueue(new CrawlJob { Url = seedURL, NextAttempt = DateTime.UtcNow});
        _ = dispatcher.Start(cts.Token);
        await Task.Delay(1000);
        cts.Cancel();

        // Assert
        int robotsTxtCalls = callTracker.VisitedUrls.Count(url => url.Equals("/robots.txt"));
        Assert.Equal(1, robotsTxtCalls); // Assert that robots.txt was called exactly once

        Assert.Contains(callTracker.VisitedUrls, url => url.Equals("/robots.txt"));
        Assert.Contains(callTracker.VisitedUrls, url => url.Equals("/public.html"));
        Assert.Contains(callTracker.VisitedUrls, url => url.Contains("/ignoreThisRule/"));
        Assert.DoesNotContain(callTracker.VisitedUrls, url => url.Equals("/private/"));

        // Assert ignore rule log registered
        loggerMock.Verify(l => l.Log(
            logLevel: LogLevel.Information,
            eventId: It.IsAny<EventId>(),
            state: It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("http://localhost/ignoreThisRule/ignored.html") &&
                v.ToString()!.Contains("localhost") 
            ),  
            exception: null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once); 

        
        // Assert blocked rule log registered
        loggerMock.Verify(l => l.Log(
            logLevel: LogLevel.Information,
            eventId: It.IsAny<EventId>(),
            state: It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("http://localhost/private/secret.html") &&
                v.ToString()!.Contains("localhost")  
            ),  
            exception: null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once); 
    }    

    [Fact]
    [Trait("TestCase", "TC-FRQ-1008")]
    public async Task Crawler_ConfigurableWhitelistUpdate()
    {
        // Arrange 
        // includes domain external-domain.com
        string seedURL = "http://localhost/SeedIncludingExternalUrl.html"; 

        CrawlJob crawlJob1 = new CrawlJob { Url = seedURL, NextAttempt = DateTime.UtcNow };

        int timeoutMs = 4000;
        int elapsedTime = 0;

        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);


        using var httpClientForCrawler = _webHostBuilder.CreateFakeInternetClient();
        
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory<TplCrawlJobDispatcher>(
            indexerMock: indexerMock, 
            httpClientForCrawler : httpClientForCrawler,
            seedUrl: seedURL
        );
        

        // Retrieve the actually implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetService<ICrawlJobDispatcher>();

        var cts = new CancellationTokenSource();    
        
        await dispatcher!.Enqueue(crawlJob1);

        // Act 1 -- Verify that none whitelisted domain is blocked
        try
        {
            _ = dispatcher.Start(cts.Token);
            
           // Wait up to 4 sec to fill list 
            while (visitedList.Count < 4 && elapsedTime < timeoutMs)
            {
                await Task.Delay(100); // wait with 100 ms intervals
                elapsedTime += 100;
            }

            Assert.DoesNotContain(visitedList, im => im.Url.Contains("external-domain.com"));
        
            // Reset measuring variables
            visitedList.Clear();
            elapsedTime = 0;

            // Act 2 -- Verify that updated whitelist domain is now allowed 
            string updatedFakeAppSettings = $$$"""
            {
                "CrawlerSettings": {
                    "UserAgent": "TestBot",
                    "MaxConcurrencyPerDomain": 2,
                    "MinDelayMs": 10,
                    "RetryIntervals": ["00:00:01"],
                    "SeedUrls": ["{{{seedURL}}}"],
                    "WhiteList": ["external-domain.com", "localhost"]
                }
            }
            """;

            CrawlJob crawlJob2 = new CrawlJob{Url = seedURL, NextAttempt = DateTime.UtcNow};

            File.WriteAllText(_tempSettingsPath, updatedFakeAppSettings);
            
            await Task.Delay(1000);
            await dispatcher!.Enqueue(crawlJob2);

            // Wait up to 4 sec to fill list 
            while (visitedList.Count < 5 && elapsedTime < timeoutMs)
            {
                await Task.Delay(100); // wait with 100 ms intervals
                elapsedTime += 100;
            }

            // Assert 
            Assert.Contains(visitedList, im => im.Url.Contains("external-domain.com"));    
        }
        finally
        {
            cts.Cancel();
        }
    }


    [Fact]
    [Trait("TestCase", "TC-NFRQ-6006")]
    public async Task Crawler_HotSwapCrawlerConfigurations()
    {
        // Arrange 
        // includes domain external-domain.com
        string ExpectedUserAgent = "UpdatedTestBot";
        string ExpectedSeedURL = "http://localhost/SeedIncludingExternalUrl.html"; 
        int ExpectedMaxConcurrencyPerDomain = 4;
        int ExpectedMinDelayMs = 100;
        string ExpectedWhiteList1 = "external-domain.com";
        string ExpectedWhiteList2 = "localhost";

        var indexerMock = new Mock<IIndexer>();
        using var httpClientForCrawler = _webHostBuilder.CreateFakeInternetClient();
        
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory<TplCrawlJobDispatcher>(
            indexerMock: indexerMock, 
            httpClientForCrawler: httpClientForCrawler
        );

        // Retrieve the actually implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var settingsLoader = scope.ServiceProvider.GetRequiredService<ICrawlerSettingsLoader>();


        var cts = new CancellationTokenSource();    
        
        // Act  
        string updatedFakeAppSettings = $$$"""
        {
            "CrawlerSettings": {
                "UserAgent": "{{{ExpectedUserAgent}}}",
                "MaxConcurrencyPerDomain": {{{ExpectedMaxConcurrencyPerDomain}}},
                "MinDelayMs": {{{ExpectedMinDelayMs}}},
                "RetryIntervals": ["00:00:02"],
                "SeedUrls": ["{{{ExpectedSeedURL}}}"],
                "WhiteList": ["{{{ExpectedWhiteList1}}}", "{{{ExpectedWhiteList2}}}"]
            }
        }
        """;

        
        File.WriteAllText(_tempSettingsPath, updatedFakeAppSettings);
        
        await Task.Delay(1000);

        // Assert 
        Assert.Equal(ExpectedUserAgent, settingsLoader.Load().UserAgent);           
        Assert.Equal(ExpectedMaxConcurrencyPerDomain, settingsLoader.Load().MaxConcurrencyPerDomain);           
        Assert.Equal(ExpectedMinDelayMs, settingsLoader.Load().MinDelayMs);           
        Assert.Equal(ExpectedSeedURL, settingsLoader.Load().SeedUrls[0]);           
        Assert.Equal(ExpectedWhiteList1, settingsLoader.Load().WhiteList[0]);           
        Assert.Equal(ExpectedWhiteList2, settingsLoader.Load().WhiteList[1]);           
    }

    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath);
    }
}
