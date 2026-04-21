using LTU.SearchEngine.Backend.Api;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure.Configurations;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Tests.Helpers;
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
        await dispatcher.Enqueue(new CrawlJob { Url = seedUrl, NextAttempt = DateTime.UtcNow });

        var cts = new CancellationTokenSource();

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using var dbContext = dbFactory.CreateDbContext();
        dbContext.Database.EnsureCreated();

        // Act 
        Task dispatchTask = dispatcher.Start(cts.Token);

        await ASecondsWait();

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
            .FirstAsync(); ;

        var term1PageWordLink = await dbContext.PageWordFrequencies
            .Include(pwf => pwf.Page)
            .Include(pwf => pwf.Term)
            .Where(pwf => pwf.Term.Id.Equals(term1Id))
            .ToListAsync();

        var term1PageAssociation = term1PageWordLink
            .Where(pwl => pwl.TermId.Equals(term1Id))
            .Select(pwl => pwl.Page.Url);

        Assert.Contains("http://localhost/InvertedIndexTestFile1.html", term1PageAssociation);
        Assert.Contains("http://localhost/invertedindextestfile2.html", term1PageAssociation);
    }


    [Fact]
    [Trait("TestCase", "TC-FRQ-2003")]
    public async Task IncrementalUpdate_ShouldOnlyUpdateModifiedPages()
    {
        // Arrange
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
    
        var urlA = "http://localhost/page-a.html";
        var urlB = "http://localhost/page-b.html";


        _webHostBuilder.DynamicContent[urlA] = "<html><body><h1>First Version</h1></body></html>";
        _webHostBuilder.DynamicContent[urlB] = "<html><body><h1>Page B (static content)</h1></body></html>";


        using var testFactory = CreateTestFactory<Indexer>(
            httpClient: httpClient, 
            seedUrl: urlA,
            maxConcurrencyPerDomain: 1
        );

        using var scope = testFactory.Services.CreateScope();

        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
        
        await dispatcher.Enqueue(new CrawlJob { Url = urlA , NextAttempt = DateTime.UtcNow });
        await dispatcher.Enqueue(new CrawlJob { Url = urlB , NextAttempt = DateTime.UtcNow });

        var cts = new CancellationTokenSource();

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using (var db = dbFactory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
        }
        
        // Act **1** 
        Task dispatchTask = dispatcher.Start(cts.Token);

        await TestWait.UntilTrue(async () =>
            {
                // try catch used to catch concurrency issues because of SQLite in-memory concurrency issues
                try 
                {
                    using var db = dbFactory.CreateDbContext();
                    return await db.PageWordFrequencies.AnyAsync(pw => 
                        pw.Page.Url.Equals(urlA) && 
                        pw.Term.Word.Equals("first")
                    );
                }
                catch (Exception) 
                {
                    return false; 
                }
            }, 
            maxWaitMs: 10000 
        );


        // Assert **1**
        IEnumerable<Term> pageATerms1 = null!;
        await TestWait.UntilTrue(async () => 
        {
            pageATerms1 = await GetTermsForUrlAsync(dbFactory, urlA);
            return pageATerms1.Any(t => t.Word == "first");
        }, maxWaitMs: 10000); 

 
        var pageAFirstAttempt = await GetLastCrawledAsync(dbFactory, urlA);
        var pageBFirstAttempt = await GetLastCrawledAsync(dbFactory, urlB);
        
        Assert.Contains(pageATerms1, t => t.Word.Equals("first"));
        Assert.DoesNotContain(pageATerms1, t => t.Word.Equals("second"));
        
        cts.Cancel();
        try { await dispatchTask; } catch (OperationCanceledException) { } 

        // Act **2**
        _webHostBuilder.DynamicContent[urlA] = "<html><body><h1>Second Version</h1></body></html>";
        await dispatcher.Enqueue(new CrawlJob { Url = urlA , NextAttempt = DateTime.UtcNow });

        var cts2 = new CancellationTokenSource();
        Task dispatchTask2 = dispatcher.Start(cts2.Token);
        
        await TestWait.UntilTrue(async () =>
            {
                using var db = dbFactory.CreateDbContext();
                
                return await db.PageWordFrequencies.AnyAsync(pw => 
                    pw.Page.Url.Equals(urlA) && 
                    pw.Term.Word.Equals("second")
                );
            }, 
            maxWaitMs: 10000 
        );
        
        
        // Assert **2**
        IEnumerable<Term> pageATerms2 = Array.Empty<Term>();


        await TestWait.UntilTrue(async () => 
        {
            pageATerms2 = await GetTermsForUrlAsync(dbFactory, urlA);
            return pageATerms2.Any(); 
        }, maxWaitMs: 5000);

        var pageASecondAttempt = await GetLastCrawledAsync(dbFactory, urlA);
        var pageBSecondAttempt = await GetLastCrawledAsync(dbFactory, urlB);
        
        cts2.Cancel();
        try { await dispatchTask2; } catch (OperationCanceledException) { }         


        Assert.DoesNotContain(pageATerms2, t => t.Word.Equals("first"));
        Assert.Contains(pageATerms2, t => t.Word.Equals("second"));
        Assert.NotEqual(pageAFirstAttempt, pageASecondAttempt);
        Assert.Equal(pageBFirstAttempt, pageBSecondAttempt);
    }

    private async Task<List<Term>> GetTermsForUrlAsync(IDbContextFactory<SearchDbContext> dbFactory, string url)
    {
        using var dbContext = dbFactory.CreateDbContext();
        
        return await dbContext.PageWordFrequencies
            .AsNoTracking() 
            .Include(pw => pw.Term)
            .Include(pw => pw.Page)
            .Where(pw => pw.Page.Url == url)
            .Select(t => t.Term)
            .ToListAsync();
    }

    private async Task<DateTime?> GetLastCrawledAsync(IDbContextFactory<SearchDbContext> dbFactory, string url)
    {
        using var dbContext = dbFactory.CreateDbContext();

        return await dbContext.Pages
            .AsNoTracking() 
            .Where(p => p.Url == url)
            .Select(p => (DateTime?)p.LastCrawled) 
            .FirstOrDefaultAsync();
    }


    [Fact]
    [Trait("TestCase", "TC-FRQ-2006")]
    public async Task Integration_CrawlShouldUpdateJobAndQueue_EvenWhenFetchFails()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TplCrawlJobDispatcher>>();
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
    
        var url = "http://localhost/bad-page.html";
        _webHostBuilder.DynamicContent[url] = "<html><body><h1>Content</h1></body></html>";

        int requestCount = 0;
        // Says to server, when you get this call, throw exception instead.
        _webHostBuilder.OnRequestReceived = (requestUrl) =>
        {
            if (requestUrl.Equals(url)) 
            {
                requestCount++;
                
                // Crude way of allowing next attempt to succeed.
                if (requestCount < 2) throw new HttpRequestException("Simulated connection reset");
            }
            return Task.CompletedTask;
        };
        
        using var testFactory = CreateTestFactory<TplCrawlJobDispatcher>(
            httpClient: httpClient, 
            seedUrl: url,
            maxConcurrencyPerDomain: 1,
            logger: loggerMock
        );

        using var scope = testFactory.Services.CreateScope();

        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
        
        await dispatcher.Enqueue(new CrawlJob { Url = url , NextAttempt = DateTime.UtcNow, RetryCount = 3 });


        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using var dbContext = dbFactory.CreateDbContext();
        dbContext.Database.EnsureCreated();
        
        // Act **1**
        var cts = new CancellationTokenSource();
        Task dispatchTask = dispatcher.Start(cts.Token);
        
        await TestWait.UntilTrue(maxWaitMs: 2000);
        cts.Cancel();

        // Assert **1**
        var pageCount = await dbContext.Pages.CountAsync();
        Assert.Equal(0, pageCount);
        

        // Verify failed fetch
        loggerMock.Verify(l => l.Log(
            logLevel: LogLevel.Error,
            eventId: It.IsAny<EventId>(),
            state: It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"failed: fetch error")),  
            exception: It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once); 
        
        // Verify attempt att retry
        loggerMock.Verify(l => l.Log(
            logLevel: LogLevel.Warning,
            eventId: It.IsAny<EventId>(),
            state: It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"reached max retry count")),  
            exception: It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once); 


        // Act **2**
        var cts2 = new CancellationTokenSource();
        
        await dispatcher.Enqueue(new CrawlJob { Url = url , NextAttempt = DateTime.UtcNow, RetryCount = 1 });
        Task dispatchTask2 = dispatcher.Start(cts2.Token);
        
        await TestWait.UntilTrue(async () =>
        {
            using var db = dbFactory.CreateDbContext();
            return await db.Pages.CountAsync() > 0;
        }, maxWaitMs: 5000);

        cts2.Cancel();

        // Assert **2**
        pageCount = await dbContext.Pages.CountAsync();
        Assert.Equal(1, pageCount);
    }   


    [Theory]
    [InlineData("/IndexerNormalizingTextRun.html", "run", 5, "en")]
    [InlineData("/IndexerNormalizingTextSwim.html", "swim", 5, "en")]
    [InlineData("/IndexerNormalizingTextCat.html", "cat", 5, "en")]
    [InlineData("/IndexerNormalizingTextEat.html", "eat", 5, "en")]
    [InlineData("/IndexerNormalizingTextHäst.html", "häst", 8, "sv")]
    [InlineData("/IndexerNormalizingTextArt.html", "art", 5, "sv")]
    [Trait("TestCase", "TC-FRQ-2007")]
    public async Task Indexer_TextIsNormalizedOnIndex(
        string url, string expectedTerm, int expectedFrequency, string languageCode)
    {
        // Arrange
        string seedUrl = $"http://localhost{url}";
        var httpClient = _webHostBuilder.CreateFakeInternetClient();
        
        using var testFactory = CreateTestFactory<Indexer>(httpClient: httpClient, seedUrl: seedUrl);
        using var scope = testFactory.Services.CreateScope();
        
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICrawlJobDispatcher>();
        await dispatcher.Enqueue(new CrawlJob{ Url = seedUrl, NextAttempt = DateTime.UtcNow });

        var cts = new CancellationTokenSource();
        
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        using var dbContext = dbFactory.CreateDbContext();
        dbContext.Database.EnsureCreated();
        
        // Act 
        Task dispatchTask = dispatcher.Start(cts.Token);
        
        await ASecondsWait();
        await ASecondsWait();

        cts.Cancel();
        
        // Assert 
        // Each unique term is stored exact1ly once in the index.
        var termPageWordLink = await dbContext.PageWordFrequencies
            .Include(pwf => pwf.Page)
            .Include(pwf => pwf.Term)
            .ToListAsync(); 

        var termLanguage = await dbContext.Terms
                .Where(t => t.Word.Equals(expectedTerm))
                .Select(t => t.LanguageCode)
                .FirstOrDefaultAsync();

        Assert.Equal(expectedTerm, termPageWordLink.First().Term.Word);
        Assert.Equal(expectedFrequency, termPageWordLink.First().HeaderFrequency);
        Assert.Equal(languageCode, termLanguage);
    }

    private static async Task ASecondsWait()
    {
        int timeoutMs = 1000;
        int elapsedTime = 0;

        while (elapsedTime < timeoutMs)
        {
            elapsedTime += 100;
            await Task.Delay(100);
        }
    }


    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath);
    }
}