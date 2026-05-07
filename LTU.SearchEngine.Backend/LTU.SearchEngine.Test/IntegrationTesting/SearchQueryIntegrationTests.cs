using System.Net.Http.Json;
using LTU.SearchEngine.Application.QueryParsing;
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
  
  
    [Fact]
    [Trait("TestCase", "TC-FRQ-3002")]
    public async Task Search_ShouldHandleSingleTermsOfDifferentLanguageButSameStem_Correctly()
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
        
        var termHello = new Term { Word = "hello", LanguageCode = "en" };
        var termTestEn = new Term { Word = "test", LanguageCode = "en" };
        var termTestSv = new Term { Word = "test", LanguageCode = "sv" };

        db.Pages.AddRange(docA, docB, docC);
        db.Terms.AddRange(termHello, termTestEn, termTestSv);
        await db.SaveChangesAsync();

        // Map terms to pages
        db.PageWordFrequencies.AddRange(
            new PageWordFrequency { Page = docA, Term = termTestSv, HeaderFrequency = 1 }, // Doc A: test Sv
            new PageWordFrequency { Page = docB, Term = termHello, HeaderFrequency = 1 }, // Doc B: hello
            new PageWordFrequency { Page = docC, Term = termTestEn, HeaderFrequency = 1 } // Doc C: test En
        );
        await db.SaveChangesAsync();

        var apiClient = testFactory.CreateClient();

        // Act & Assert: Step 1 - ("hello")
        var url = SearchUrlGenerator.QueryUrlBuilder(query: "hello", language: "en");
        var helloResult = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);
        
        
        Assert.Contains(helloResult!.searchResults, r => r.Url.Contains("/b"));
        Assert.DoesNotContain(helloResult.searchResults, r => r.Url.Contains("/c"));
        Assert.DoesNotContain(helloResult.searchResults, r => r.Url.Contains("/a"));

        // Act & Assert: Step 2 - ("test")
        url = SearchUrlGenerator.QueryUrlBuilder(query: "test", language: "en");
        var testResult = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);
        
        Assert.Equal(2, testResult!.searchResults.Count());
        Assert.Contains(testResult.searchResults, r => r.Url.Contains("/a"));
        Assert.Contains(testResult.searchResults, r => r.Url.Contains("/c"));
        Assert.DoesNotContain(testResult.searchResults, r => r.Url.Contains("/b"));
    }
   

    [Theory]
    [InlineData("+")]
    [InlineData("!")]
    [InlineData("%")]
    [InlineData("#")]
    [InlineData("&")]
    [InlineData("|")]
    [InlineData("@")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("\"")]
    [Trait("TestCase", "TC-FRQ-3002")]
    public async Task Search_ShouldHandleEscapedCharacters_Correctly(string character)
    {
        // Arrange
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
        using var testFactory = CreateTestFactory<Indexer>(httpClient: httpClient);
        using var scope = testFactory.Services.CreateScope();
        
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        // Seed Documents A
        var docA = new Page { Url = "http://localhost/a", Title = "Doc A", LastCrawled = DateTime.UtcNow };
        
        // term will look like this ex: te!rm
        var term = new Term { Word = $"te{character}rm", LanguageCode = "en" };
        
        db.Pages.AddRange(docA);
        db.Terms.AddRange(term);
        await db.SaveChangesAsync();

        // Map terms to pages
        db.PageWordFrequencies.AddRange(
            new PageWordFrequency { Page = docA, Term = term, HeaderFrequency = 1 }
        );

        await db.SaveChangesAsync();

        var apiClient = testFactory.CreateClient();

        // Act - query will look like this ex: te\!rm
        var url = SearchUrlGenerator.QueryUrlBuilder(query: $"te\\{character}rm", language: "en");
        var helloResult = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);
        
        // Assert
        Assert.Contains(helloResult!.searchResults, r => r.Url.Contains("/a"));
    }
    


    [Theory]
    [InlineData("\"hello from page\"")]
    [InlineData("\"hello from\"")]
    [InlineData("\"from page\"")]
    [InlineData("hello AND \"from page\"")]
    [InlineData("\"hello from\" AND page")]
    [Trait("TestCase", "TC-FRQ-3003")]
    public async Task Search_ShouldHandlePhraseQueries_Correctly(string input)
    {
        // Arrange
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
        using var testFactory = CreateTestFactory<Indexer>(httpClient: httpClient);
        using var scope = testFactory.Services.CreateScope();
        
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        // Seed Documents A, B, and C
        var docA = new Page { Url = "http://localhost/a", Title = "Doc A", LastCrawled = DateTime.UtcNow }; // Hello from page 
        var docB = new Page { Url = "http://localhost/b", Title = "Doc B", LastCrawled = DateTime.UtcNow }; // Hello Page from
        var docC = new Page { Url = "http://localhost/c", Title = "Doc C", LastCrawled = DateTime.UtcNow }; // Hello wonderer
        
        var termHello = new Term { Word = "hello", LanguageCode = "en" };
        var termFrom = new Term { Word = "from", LanguageCode = "en" };
        var termPage = new Term { Word = "page", LanguageCode = "en" };
        var termWonderer = new Term { Word = "wonderer", LanguageCode = "en" };
        
        db.Pages.AddRange(docA, docB, docC);
        db.Terms.AddRange(termHello, termFrom, termPage, termWonderer);
        await db.SaveChangesAsync();

        // Map terms to pages
        db.PageWordFrequencies.AddRange(
            // Hello from page
            new PageWordFrequency { Page = docA, Term = termHello, HeaderFrequency = 1 }, 
            new PageWordFrequency { Page = docA, Term = termFrom, HeaderFrequency = 1 }, 
            new PageWordFrequency { Page = docA, Term = termPage, HeaderFrequency = 1 }, 
            // Hello page from 
            new PageWordFrequency { Page = docB, Term = termHello, HeaderFrequency = 1 }, 
            new PageWordFrequency { Page = docB, Term = termPage, HeaderFrequency = 1 }, 
            new PageWordFrequency { Page = docB, Term = termFrom, HeaderFrequency = 1 }, 
            // Hello Wonderer
            new PageWordFrequency { Page = docC, Term = termHello, HeaderFrequency = 1 }, 
            new PageWordFrequency { Page = docC, Term = termWonderer, HeaderFrequency = 1 } 
        );

        db.PageWordPositions.AddRange(
            // Hello from page
            new PageWordPosition { Page = docA, Term = termHello, Position = 0},
            new PageWordPosition { Page = docA, Term = termFrom, Position = 1},
            new PageWordPosition { Page = docA, Term = termPage, Position = 2},
            // Hello page from
            new PageWordPosition { Page = docB, Term = termHello, Position = 0},
            new PageWordPosition { Page = docB, Term = termPage, Position = 1},
            new PageWordPosition { Page = docB, Term = termFrom, Position = 2},
            // Hello Wonderer 
            new PageWordPosition { Page = docC, Term = termHello, Position = 0},
            new PageWordPosition { Page = docC, Term = termWonderer, Position = 1}
        );

        await db.SaveChangesAsync();

        var apiClient = testFactory.CreateClient();

        // Act & Assert: Step 1 - ("Hello from page A")
        var url = SearchUrlGenerator.QueryUrlBuilder(query: input, language: "en");
        var helloResult = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);
        
        Assert.Contains(helloResult!.searchResults, r => r.Url.Contains("/a"));
        Assert.DoesNotContain(helloResult.searchResults, r => r.Url.Contains("/b"));
        Assert.DoesNotContain(helloResult.searchResults, r => r.Url.Contains("/c"));
    }

    private async Task<HttpClient> SetupSearchBooleanOperatorAreCaseSensitiveDatabase(WebApplicationFactory<Program> testFactory)
    {
        using var scope = testFactory.Services.CreateScope();
        
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        // Seed Documents A, B, and C
        var docA = new Page { Url = "http://localhost/a", Title = "Doc A", LastCrawled = DateTime.UtcNow }; // term1 term2
        
        var term1 = new Term { Word = "term1", LanguageCode = "en" };
        var term2 = new Term { Word = "term2", LanguageCode = "en" };
        
        db.Pages.AddRange(docA);
        db.Terms.AddRange(term1, term2);
        await db.SaveChangesAsync();

        // Map terms to pages
        db.PageWordFrequencies.AddRange(
            new PageWordFrequency { Page = docA, Term = term1, HeaderFrequency = 1 }, 
            new PageWordFrequency { Page = docA, Term = term2, HeaderFrequency = 1 } 
        );

        await db.SaveChangesAsync();

        return testFactory.CreateClient();
    }


    [Theory]
    [InlineData("and", "and", "AND")]
    [InlineData("And", "and", "AND")]
    [InlineData("ANd", "and", "AND")]
    [InlineData("aND", "and", "AND")]
    [InlineData("anD", "and", "AND")]
    [InlineData("or", "or", "OR")]
    [InlineData("Or", "or", "OR")]
    [InlineData("oR", "or", "OR")]
    [Trait("TestCase", "TC-FRQ-3004")]
    public async Task Search_BooleanOperatorAreCaseSensitiveAndOr_Correctly(
        string input, 
        string expectedIgnored, 
        string correctOperator
        )
    {
        // Arrange
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
        using var testFactory = CreateTestFactory<QueryParser>(httpClient: httpClient);
        var apiClient = await SetupSearchBooleanOperatorAreCaseSensitiveDatabase(testFactory);

        // Act - Step 1 - Not capital letters 
        string query = $"term1 {input} term2";
        var url = SearchUrlGenerator.QueryUrlBuilder(query, language: "en");
        var result = await apiClient .GetFromJsonAsync<SearchResponseDTO>(url);
        
        // Assert
        Assert.Contains(result!.searchResults, r => r.Url.Contains("/a"));
        Assert.Contains(result.ignoredTokens!, r => r.Token.Contains(expectedIgnored));
        
        // Act - Step 2 - Capital letters 
        query = $"term1 {correctOperator} term2";
        url = SearchUrlGenerator.QueryUrlBuilder(query, language: "en");
        result = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);
        
        // Assert
        Assert.Contains(result!.searchResults, r => r.Url.Contains("/a"));
        Assert.DoesNotContain(result.ignoredTokens!, r => r.Token.Contains(expectedIgnored));    
    }


    [Theory]
    [InlineData("not", "not", "NOT")]
    [InlineData("Not", "not", "NOT")]
    [InlineData("NOt", "not", "NOT")]
    [InlineData("nOT", "not", "NOT")]
    [InlineData("noT", "not", "NOT")]
    [Trait("TestCase", "TC-FRQ-3004")]
    public async Task Search_BooleanOperatorAreCaseSensitiveNot_Correctly(
        string input, 
        string expectedIgnored, 
        string correctOperator
        )
    {
        // Arrange
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
        using var testFactory = CreateTestFactory<QueryParser>(httpClient: httpClient);
        var apiClient = await SetupSearchBooleanOperatorAreCaseSensitiveDatabase(testFactory);

        // Act - Step 1 - Not capital letters 
        string query = $"term1 {input} term2";
        var url = SearchUrlGenerator.QueryUrlBuilder(query, language: "en");
        var result = await apiClient .GetFromJsonAsync<SearchResponseDTO>(url);
        
        // Assert
        Assert.Contains(result!.searchResults, r => r.Url.Contains("/a"));
        Assert.Contains(result.ignoredTokens!, r => r.Token.Contains(expectedIgnored));
        
        // Act - Step 2 - Capital letters 
        query = $"term1 {correctOperator} term2";
        url = SearchUrlGenerator.QueryUrlBuilder(query, language: "en");
        result = await apiClient.GetFromJsonAsync<SearchResponseDTO>(url);
        
        // Assert
        Assert.DoesNotContain(result!.searchResults, r => r.Url.Contains("/a"));
        Assert.DoesNotContain(result.ignoredTokens!, r => r.Token.Contains(expectedIgnored));
    }


    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath);
    }
}