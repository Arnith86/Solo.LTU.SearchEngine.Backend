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
    private string _seedURL;
    private string _page1;
    private string _page2;
    private string _final;
     
    public CrawlerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _seedURL = "http://localhost/seed.html";
        _page1 = "http://localhost/page1.html";
        _page2 = "http://localhost/page2.html";
        _final = "http://localhost/final.html";
        _factory = factory;
    } 

    [Fact]
    [Trait("TestCase", "TC-FRQ-1001")]
    public async Task Crawler_ShouldVisitAllLinkedPagesRecursivly()
    {
        // Arrange 
        var visitedList = new List<CrawlResult>();
        var indexerMock = new Mock<IIndexer>();

        indexerMock
            .Setup(im => im.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => visitedList.Add(result))
            .Returns(Task.CompletedTask);

        var webHelper = new HelperClasses.WebHostBuilder();
        using var httpClientForCrawler = webHelper.BuildHttpClient();


        // Creates a version of the application, but with a mock index instead.
        var testFactory =_factory.WithWebHostBuilder(builder =>
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
                    ["CrawlerSettings:SeedUrls:0"] = _seedURL,
                    ["CrawlerSettings:SeedUrls:1"] = null
                };

                config.AddInMemoryCollection(testConfig);
            });
        });

        // Retrieve the actuall implementation of the crawler. 
        using var scope = testFactory.Services.CreateScope();
        var crawler = scope.ServiceProvider.GetRequiredService<ICrawler>();


        // Act
        await crawler.FetchAsync(_seedURL);
        
        int timeoutMs = 5000; 
        int elapsedTime = 0;

        // Wait up to 5 sec to fill list 
        while(visitedList.Count < 4 && elapsedTime < timeoutMs)
        {
            await Task.Delay(100); // wait with 100 ms intervalls
            elapsedTime += 100;
        }

        // Assert
        Assert.Equal(4, visitedList.Count);
        Assert.Equal(_seedURL, visitedList[0].Url);
        Assert.Equal(_page1, visitedList[1].Url);
        Assert.Equal(_page2, visitedList[2].Url);
        Assert.Equal(_final, visitedList[3].Url);
    }
}