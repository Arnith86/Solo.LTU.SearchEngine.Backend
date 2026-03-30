using Castle.Components.DictionaryAdapter.Xml;
using LTU.SearchEngine.Backend.Api;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Configurations;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Indexing;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace LTU.SearchEngine.Test.IntegrationTesting;

public class IndexerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory; 
    private readonly HelperClasses.WebHostBuilder _webHostBuilder;
    private string _tempSettingsPath; 
    

    public IndexerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _webHostBuilder = new HelperClasses.WebHostBuilder();
         _tempSettingsPath = 
            Path.Combine(Path.GetTempPath(), $"Settings_{Guid.NewGuid()}.ToJsonSchemaType");
    }


    private WebApplicationFactory<Program> CreateTestFactory<T>(
        HttpClient httpClient,
        Mock<ILogger<T>>? logger = null,
        string seedUrl = "http://localhost/page1.html",
        int maxConcurrencyPerDomain = 2,
        int minDelayMs = 10,
        Mock<IRobotsHandler>? robotsHandlerMock = null
    ) where T : class
    {
        // Creates an in-memory SQLite connection for the test database.
        SqliteConnection sqliteConnection = new SqliteConnection("Filename=:memory:");
        sqliteConnection.Open();

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

                // Removes everything related to the actual database context and replaces it with an in-memory SQLite version.
                var dbContextDescriptors = services.Where(d => 
                    d.ServiceType.FullName!.Contains("EntityFrameworkCore")||
                    d.ServiceType == typeof(SearchDbContext) ||
                    d.ServiceType.Name.Contains("IDbContextFactory")).ToList();

                foreach (var d in dbContextDescriptors) services.Remove(d);

                services.AddDbContextFactory<SearchDbContext>(options => 
                {
                    options.UseSqlite(sqliteConnection);
                    options.UseInternalServiceProvider(null); // Avoids issues with multiple service providers in tests
                });
                
                services.AddSingleton<HttpClient>(httpClient); // Replace the HttpClient the client uses to the in-memory webHostBuilder
                if (logger is not null) services.AddSingleton(logger.Object);
                if (robotsHandlerMock is not null) services.AddSingleton(robotsHandlerMock.Object);
            });

        });

        return testFactory;
    } 

    [Fact]
    [Trait("TestCase", "TC-FRQ-2002")]
    public async Task Indexer_IndexedTermsStoredWithWithReferenceToPagesTheyAppearOn_UsingInvertedIndex()
    {
        // Included terms in test files:
        //  Page1                       Page2
        //  -------------------------------------------------
        //  Term1                   |   Term1
        //                          |   Term2    
        //  Term3                   |   
        //  InvertedIndexTestFile2  |
        
        // Arrange
        string seedUrl = "http://localhost/InvertedIndexTestFile1.html";
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
        
        using var testFactory = CreateTestFactory<Indexer>(httpClient: httpClient, seedUrl: seedUrl);
        using var scope = testFactory.Services.CreateScope();
        
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
        await dispatcher.Enqueue(new CrawlJob{Url = seedUrl, NextAttempt = DateTime.UtcNow});

        var cts = new CancellationTokenSource();
        
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using var dbContext = dbFactory.CreateDbContext();
        dbContext.Database.EnsureCreated();
        
        // Act 
        Task dispatchTask = dispatcher.Start(cts.Token);
        
        int timeoutMs = 5000;
        int elapsedTime = 0;
        
        while (elapsedTime < timeoutMs)
        {
            elapsedTime += 100;
            await Task.Delay(100);
        }

        cts.Cancel();


        
        // Assert 
        // Each unique term is stored exactly once in the index.
        var TotalTermCount = await dbContext.Terms.CountAsync();
        var Term1Count = await dbContext.Terms.Where(t => t.Word.Equals("term1")).CountAsync();
        var Term2Count = await dbContext.Terms.Where(t => t.Word.Equals("term2")).CountAsync();
        var Term3Count = await dbContext.Terms.Where(t => t.Word.Equals("term3")).CountAsync();

        Assert.True(TotalTermCount.Equals(4));
        Assert.True(Term1Count.Equals(1));
        Assert.True(Term2Count.Equals(1));
        Assert.True(Term3Count.Equals(1));
        
        // Each term maps to one or more page references (URLs or document IDs).
        // Pages containing the same term are associated with that term.
        int term1Id = await dbContext.Terms
            .Where(t => t.Word.Equals("term1"))
            .Select(t => t.Id)
            .FirstAsync();;
     
        var term1PageWordLink = await dbContext.PageWordFrequencies
            .Include(pwf => pwf.Page)
            .Include(pwf => pwf.Term)
            .Where(pwf => pwf.Term.Id.Equals(term1Id))
            .ToListAsync(); 

        var term1PageAssociation = term1PageWordLink
            .Where(pwl => pwl.TermId.Equals(term1Id))
            .Select(pwl => pwl.Page.Url);

        Assert.Contains("http://localhost/InvertedIndexTestFile1.html", term1PageAssociation);
        Assert.Contains("http://localhost/InvertedIndexTestFile2.html", term1PageAssociation);
}
    

    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath);
    }
}