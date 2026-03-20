using LTU.SearchEngine.Backend.Api;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LTU.SearchEngine.Test.IntegrationTesting;


public class CrawlerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory; 
    private readonly WebHostBuilder _webHostBuilder;
    private string _tempSettingsPath; 
     
    public CrawlerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _webHostBuilder = new WebHostBuilder();
         _tempSettingsPath = 
            Path.Combine(Path.GetTempPath(), $"Settings_{Guid.NewGuid()}.ToJsonSchemaType");
    } 

    private WebApplicationFactory<Program> CreateTestFactory(
        string initialJson,
        Mock<IIndexer> indexerMock,
        HttpClient httpClientForCrawler)
    {
        File.WriteAllText(_tempSettingsPath, initialJson);

        // Creates a version of the application, but with a mock index instead.
        var testFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
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

        string fakeAppSettings = $$$"""
        {
            "CrawlerSettings": {
                "UserAgent": "TestBot",
                "MaxConcurrencyPerDomain": 2,
                "MinDelayMs": 10,
                "RetryIntervals": ["00:00:01"],
                "SeedUrls": ["{{{seedURL}}}"],
                "WhiteList": ["localhost"]
            }
        }
        """;
    
        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        var webHelper = new HelperClasses.WebHostBuilder();
        using var httpClientForCrawler = webHelper.BuildHttpClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            fakeAppSettings,
            indexerMock, 
            httpClientForCrawler
        );


        // Retrieve the actuall implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
       
        // Act
        int timeoutMs = 5000;
        int elapsedTime = 0;

        // Wait up to 5 sec to fill list 
        while (visitedList.Count < 4 && elapsedTime < timeoutMs)
        {
            await Task.Delay(100); // wait with 100 ms intervalls
            elapsedTime += 100;
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

        string fakeAppSettings = $$$"""
        {
            "CrawlerSettings": {
                "UserAgent": "TestBot",
                "MaxConcurrencyPerDomain": 2,
                "MinDelayMs": 10,
                "RetryIntervals": ["00:00:01"],
                "SeedUrls": ["{{{externalURL}}}"],
                "WhiteList": ["localhost"]
            }
        }
        """;
       
        
        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        var webHelper = new HelperClasses.WebHostBuilder();
        using var httpClientForCrawler = webHelper.BuildHttpClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            fakeAppSettings,
            indexerMock,
            httpClientForCrawler
        );


        // Retrieve the actuall implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
     
        // Act
        int timeoutMs = 500;
        int elapsedTime = 0;

        // Wait up to 5 sec to fill list 
        while (elapsedTime < timeoutMs)
        {
            await Task.Delay(100); // wait with 100 ms intervalls
            elapsedTime += 100;
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
        
        string fakeAppSettings = $$$"""
        {
            "CrawlerSettings": {
                "UserAgent": "TestBot",
                "MaxConcurrencyPerDomain": 2,
                "MinDelayMs": 10,
                "RetryIntervals": ["00:00:01"],
                "SeedUrls": ["{{{seedURL}}}"],
                "WhiteList": ["localhost"]
            }
        }
        """;

        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        using var httpClientForCrawler = _webHostBuilder.CreateFakeInternetClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            fakeAppSettings,
            indexerMock, 
            httpClientForCrawler
        );


        // Retrieve the actuall implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
      

        // Act
        int timeoutMs = 4000;
        int elapsedTime = 0;

        // Wait up to 5 sec to fill list 
        while (visitedList.Count < 4 && elapsedTime < timeoutMs)
        {
            await Task.Delay(100); // wait with 100 ms intervalls
            elapsedTime += 100;
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

        string fakeAppSettings = $$$"""
        {
            "CrawlerSettings": {
                "UserAgent": "TestBot",
                "MaxConcurrencyPerDomain": 2,
                "MinDelayMs": 10,
                "RetryIntervals": ["00:00:01"],
                "SeedUrls": ["{{{seedURL}}}"],
                "WhiteList": ["localhost"]
            }
        }
        """;
        
        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);


        using var httpClientForCrawler = _webHostBuilder.CreateFakeInternetClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            fakeAppSettings,
            indexerMock, 
            httpClientForCrawler
        );


        // Retrieve the actuall implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetService<ICrawlJobDispatcher>();
        

        // Act 1 -- Verify that none whitelisted domain is blocked
        int timeoutMs = 4000;
        int elapsedTime = 0;

        // Wait up to 4 sec to fill list 
        while (visitedList.Count < 4 && elapsedTime < timeoutMs)
        {
            await Task.Delay(100); // wait with 100 ms intervalls
            elapsedTime += 100;
        }

        Assert.DoesNotContain(visitedList, im => im.Url.Contains("external-domain.com"));
        visitedList.Clear();

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

        CrawlJob crawlJob = new CrawlJob{Url = seedURL, NextAttempt = DateTime.UtcNow};

        File.WriteAllText(_tempSettingsPath, updatedFakeAppSettings);
        
        elapsedTime = 0;
        await Task.Delay(1000);
        await dispatcher!.Enqueue(crawlJob);

        // Wait up to 4 sec to fill list 
        while (visitedList.Count < 5 && elapsedTime < timeoutMs)
        {
            await Task.Delay(100); // wait with 100 ms intervalls
            elapsedTime += 100;
        }

        // Assert 
        Assert.Contains(visitedList, im => im.Url.Contains("external-domain.com"));
    }


    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath);
    }
}
