using Castle.Components.DictionaryAdapter.Xml;
using LTU.SearchEngine.Backend.Api;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Configurations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace LTU.SearchEngine.Test.IntegrationTesting;

public class HtmlParserIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory; 
    private readonly HelperClasses.WebHostBuilder _webHostBuilder;
    private string _tempSettingsPath; 
     
    public HtmlParserIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _webHostBuilder = new HelperClasses.WebHostBuilder();
         _tempSettingsPath = 
            Path.Combine(Path.GetTempPath(), $"Settings_{Guid.NewGuid()}.ToJsonSchemaType");
    }


    private WebApplicationFactory<Program> CreateTestFactory<T>(
        Mock<IIndexer> indexerMock,
        HttpClient httpClient,
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

                var descriptors = services.Where(d => d.ServiceType == typeof(HttpClient)).ToList();
                foreach (var d in descriptors) services.Remove(d);

                services.AddSingleton<HttpClient>(httpClient); // Replace the HttpClient the client uses to the in-memory webHostBuilder
                services.AddSingleton(indexerMock.Object);
                if (logger is not null) services.AddSingleton(logger.Object);
                if (robotsHandlerMock is not null) services.AddSingleton(robotsHandlerMock.Object);
            });

        });

        return testFactory;
    } 

    [Fact]
    [Trait("TestCase", "TC-FRQ-2001")]
    public async Task HtmlParser_ShouldOnlyExtractVisibleText_AndIgnoreNonTextualElements()
    {
        // Arrange
        string seedUrl = "http://localhost/ParserTestFile.html";
      
        CrawlResult capturedCrawlResult = null!; 

        Mock<IIndexer> indexerMock = new Mock<IIndexer>();
        indexerMock.Setup(i => i.IndexAsync(It.IsAny<CrawlResult>()))
            .Callback<CrawlResult>(result => capturedCrawlResult = result) 
            .Returns(Task.CompletedTask);

        using var httpClient = _webHostBuilder.CreateFakeInternetClient();

        using var testFactory = CreateTestFactory<HapHtmlParser>(
            indexerMock: indexerMock,
            httpClient: httpClient,
            seedUrl: seedUrl
        );

        using var scope = testFactory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
        var cts = new CancellationTokenSource();

        await dispatcher.Enqueue(new CrawlJob{ Url = seedUrl, NextAttempt = DateTime.UtcNow});
        
        // Act 
        Task parseTask = dispatcher.Start(cts.Token);

        int timeoutMs = 1000;
        int elapsedTime = 0;
        
        while (elapsedTime < timeoutMs)
        {
            elapsedTime += 100;
            await Task.Delay(100);
        }

        // Assert
        Assert.Contains("Title Text", capturedCrawlResult.Title);
        Assert.Contains("Headers", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.Contains("Paragraph", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.Contains("AltText", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.Contains("HiddenText", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("DOCTYPE", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("html", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("lang", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("script", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("ScriptText", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("style", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("display", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("none", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("img", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("imageName", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("video", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("controls", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("lecture", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
        Assert.DoesNotContain("mp4", capturedCrawlResult.IndexedTerms.Select(t => t.Term));
    }


    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath);
    }
}