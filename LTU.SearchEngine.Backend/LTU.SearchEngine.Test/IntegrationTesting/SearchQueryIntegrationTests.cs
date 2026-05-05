using System.Net.Http.Json;
using LTU.SearchEngine.Backend.Api;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure.Configurations;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace LTU.SearchEngine.Test.IntegrationTesting;

public class SearchQueryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory; 
    private readonly HelperClasses.WebHostBuilder _webHostBuilder;
    private string _tempSettingsPath; 
    

    public SearchQueryIntegrationTests(WebApplicationFactory<Program> factory)
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
        string dbName = Guid.NewGuid().ToString();
        SqliteConnection sqliteConnection = new SqliteConnection($"Data Source={dbName};Mode=Memory;Cache=Shared");
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

                if (sqliteConnection.State != System.Data.ConnectionState.Open)
                {
                    sqliteConnection.Open();
                }
                
                services.AddSingleton(sqliteConnection);
                services.AddDbContextFactory<SearchDbContext>((container, options) => 
                {
                    var conn = container.GetRequiredService<SqliteConnection>();
                    options.UseSqlite(conn);
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
    [Trait("TestCase", "TC-FRQ-3001")]
    public async Task Search_ShouldHandleSimpleTerms_AndBooleanAND_Correctly()
    {
        // Arrange
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
        using var testFactory = CreateTestFactory<Indexer>(httpClient: httpClient);
        using var scope = testFactory.Services.CreateScope();
        
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        // Seed Documents A, B, and C
        var docA = new Page { Url = "http://localhost/a", Title = "Doc A", LastCrawled = DateTime.UtcNow };
        var docB = new Page { Url = "http://localhost/b", Title = "Doc B", LastCrawled = DateTime.UtcNow };
        var docC = new Page { Url = "http://localhost/c", Title = "Doc C", LastCrawled = DateTime.UtcNow };
        
        var termCats = new Term { Word = "cat", LanguageCode = "en" };
        var termDogs = new Term { Word = "dog", LanguageCode = "en" };

        db.Pages.AddRange(docA, docB, docC);
        db.Terms.AddRange(termCats, termDogs);
        await db.SaveChangesAsync();

        // Map terms to pages
        db.PageWordFrequencies.AddRange(
            new PageWordFrequency { Page = docA, Term = termCats, HeaderFrequency = 1 }, // Doc A: cat
            new PageWordFrequency { Page = docB, Term = termDogs, HeaderFrequency = 1 }, // Doc B: dog
            new PageWordFrequency { Page = docC, Term = termCats, HeaderFrequency = 1 }, // Doc C: cat
            new PageWordFrequency { Page = docC, Term = termDogs, HeaderFrequency = 1 }  // Doc C: dog
        );
        await db.SaveChangesAsync();

        var apiClient = testFactory.CreateClient();

        // Act & Assert: Step 1 - Simple Term Search ("cats")
        var url = SearchUrlGenerator.QueryUrlBuilder(query: "cats", language: "en");
        var catsResult = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);
        
        Assert.NotNull(catsResult!.searchResults);
        Assert.Contains(catsResult.searchResults, r => r.Url.Contains("/a"));
        Assert.Contains(catsResult.searchResults, r => r.Url.Contains("/c"));
        Assert.DoesNotContain(catsResult.searchResults, r => r.Url.Contains("/b"));

        // Act & Assert: Step 2 - Multiple Terms ("cats dogs") implicit OR
        url = SearchUrlGenerator.QueryUrlBuilder(query: "cats dogs", language: "en");
        var multipleResult = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);
        
        Assert.Equal(3, multipleResult!.searchResults.Count());

        // Act & Assert: Step 3 - Boolean AND ("cats AND dogs") 
        url = SearchUrlGenerator.QueryUrlBuilder(query: "cats AND dogs", language: "en");
        var andResult = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);

        // Expected: Only Document C
        Assert.Single(andResult!.searchResults);
        Assert.Equal("Doc C", andResult.searchResults.First().Title);
        Assert.Contains("/c", andResult.searchResults.First().Url);
    }

    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath);
    }
}