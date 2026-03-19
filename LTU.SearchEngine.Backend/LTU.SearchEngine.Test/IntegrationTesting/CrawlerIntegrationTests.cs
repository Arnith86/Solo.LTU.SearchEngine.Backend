using LTU.SearchEngine.Backend.Api;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Crawling;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LTU.SearchEngine.Test.IntegrationTesting;

public class CrawlerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory; 
     
    public CrawlerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    } 

    private WebApplicationFactory<Program> CreateTestFactory(
        string seedURL,
        string whiteListDomain,
        Mock<IIndexer> indexerMock,
        HttpClient httpClientForCrawler)
    {
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
                var testConfig = new Dictionary<string, string?>
                {
                    ["CrawlerSettings:SeedUrls:0"] = seedURL,
                    ["CrawlerSettings:SeedUrls:1"] = null,
                    ["CrawlerSettings:WhiteList:0"] = whiteListDomain
                };

                config.AddInMemoryCollection(testConfig);
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
        
        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        var webHelper = new HelperClasses.WebHostBuilder();
        using var httpClientForCrawler = webHelper.BuildHttpClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            seedURL, 
            whiteListDomain: "localhost", 
            indexerMock, 
            httpClientForCrawler
        );


        // Retrieve the actuall implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var crawler = scope.ServiceProvider.GetRequiredService<ICrawler>();


        // Act
        await crawler.FetchAsync(seedURL);

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
        
        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        var webHelper = new HelperClasses.WebHostBuilder();
        using var httpClientForCrawler = webHelper.BuildHttpClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            externalURL, 
            whiteListDomain: "localhost", 
            indexerMock, 
            httpClientForCrawler
        );


        // Retrieve the actuall implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var crawler = scope.ServiceProvider.GetRequiredService<ICrawler>();


        // Act
        await crawler.FetchAsync(externalURL);

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
        string seedURL = "http://localhost/SeedIncludingExternalUrl.html"; // includes http://forbidden-domain.com
        string page1 = "http://localhost/page1.html";
        string page2 = "http://localhost/page2.html";
        string final = "http://localhost/final.html";
        

        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        var webHelper = new HelperClasses.WebHostBuilder();
        using var httpClientForCrawler = webHelper.BuildHttpClient();
        
        using WebApplicationFactory<Program> testFactory = CreateTestFactory(
            seedURL, 
            whiteListDomain: "localhost", 
            indexerMock, 
            httpClientForCrawler
        );


        // Retrieve the actuall implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var crawler = scope.ServiceProvider.GetRequiredService<ICrawler>();


        // Act
        await crawler.FetchAsync(seedURL);

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

        testFactory.Dispose();
    }
}
