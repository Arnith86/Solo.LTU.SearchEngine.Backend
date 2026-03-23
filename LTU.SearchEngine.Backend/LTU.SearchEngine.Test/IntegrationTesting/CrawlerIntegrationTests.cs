using LTU.SearchEngine.Backend.Api;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.BackgroundServices;
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

    private WebApplicationFactory<Program> CreateTestFactory(
        // string initialJson,
        Mock<IIndexer> indexerMock,
        HttpClient httpClientForCrawler,
        string seedUrl = "http://localhost/page1.html",
        string maxConcurrencyPerDomain = "2",
        string minDelayMs = "10"
    )
    {
        string fakeAppSettings = $$$"""
        {
            "CrawlerSettings": {
                "UserAgent": "TestBot",
                "MaxConcurrencyPerDomain": {{{maxConcurrencyPerDomain}}},
                "MinDelayMs": {{{minDelayMs}}},
                "RetryIntervals": ["00:00:01"],
                "SeedUrls": ["{{{seedUrl}}}"],
                "WhiteList": ["localhost"]
            }
        }
        """;

        File.WriteAllText(_tempSettingsPath, fakeAppSettings);

        // Creates a version of the application, but with a mock index instead.
        var testFactory = _factory.WithWebHostBuilder(builder =>
        {
            
            builder.ConfigureServices(services =>
            {
                // This finds the service that we want to remove from the application startup
                var descriptor = services.FirstOrDefault(d => 
                    d.ImplementationType == typeof(CrawlBackgroundService));

                if (descriptor is not null) services.Remove(descriptor);

                services.AddSingleton(httpClientForCrawler); // Replace the HttpClient the client uses to the inmemory webHostBuilder
                services.AddSingleton(indexerMock.Object);
            });

            // Replaces the preconfigured seed url with the test version. 
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile(_tempSettingsPath, optional: false, reloadOnChange: true);
            });   
        });

        return testFactory;
    }


    [Fact]
    [Trait("TestCase", "TC-FRQ-1001")]
    public async Task Crawler_ShouldVisitAllLinkedPagesRecursivly()
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
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            indexerMock, 
            httpClientForCrawler
        );


        // Retrieve the actuall implementation of the crawler. 
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
                await Task.Delay(100); // wait with 100 ms intervalls
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
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            indexerMock,
            httpClientForCrawler,
            externalURL
        );


        // Retrieve the actuall implementation of the crawler. 
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
                await Task.Delay(100); // wait with 100 ms intervalls
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
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            indexerMock, 
            httpClientForCrawler,
            seedURL
        );

        // Retrieve the actuall implementation of the CrawlJobDispatcher. 
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
                await Task.Delay(100); // wait with 100 ms intervalls
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
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            indexerMock, 
            httpClientForCrawler,
            seedURL
        );


        // Retrieve the actuall implementation of the crawler. 
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
                await Task.Delay(100); // wait with 100 ms intervalls
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
                await Task.Delay(100); // wait with 100 ms intervalls
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
    [Trait("TestCase", "TC-FRQ-1003-A")]
    public async Task Crawler_DomainRateAndConcurrencyLimitEnforcement()
    {
        // Arrange 
        // Setting up timing instruments
        string maxConcurrencyPerDomain = "2";

        var timesStamps = new List<DateTime>();
        int activeRequests = 0;
        int maxObservedConcurrency = 0;
        var lockObject = new Object();

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
                           maxObservedConcurrency = Math.Max(maxObservedConcurrency, current);
                           timesStamps.Add(DateTime.UtcNow);
                       }

                       // we force the connection to stay open to measure concurrency 
                       await Task.Delay(300); 
                       Interlocked.Decrement(ref activeRequests);
                       await context.Response.WriteAsync("<html><body>Done</body></html>");
                   });
                });
            }).Build();; 
        
        await host.StartAsync();

        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            new Mock<IIndexer>(), 
            host.GetTestClient(),
            maxConcurrencyPerDomain
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

        // Assert
        Assert.True(maxObservedConcurrency <= settingsLoader.Load().MaxConcurrencyPerDomain);
    }

    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath);
    }
}
