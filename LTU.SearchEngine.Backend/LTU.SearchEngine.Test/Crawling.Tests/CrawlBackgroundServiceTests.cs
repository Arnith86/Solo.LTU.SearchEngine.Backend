using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Crawling;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Infrastructure.Indexing.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using System.Net;
using System.Net.NetworkInformation;
using Xunit;
using static System.Net.Mime.MediaTypeNames;

namespace LTU.SearchEngine.Test.Crawling.Tests
{

    public class CrawlBackgroundServiceTests
    {
        [Fact]
        public async Task TC_FRQ_1001_Verify_Crawler_Visits_All_Reachable_Pages_Recursively()
        {
            // ARRANGE (Preconditions & Setup)

            // Define the chain of URLs to simulate a crawling path
            var seedUrl = "https://www.ltu.se";
            var page1Url = "https://www.ltu.se/page1";
            var page2Url = "https://www.ltu.se/page2";
            var finalPageUrl = "https://www.ltu.se/final";

            // --- Mock Crawler Setup ---
            // We simulate a scenario where the crawler finds a new link on each page.
            // This is essential to test if the system handles recursion correctly.
            var mockCrawler = new Mock<ICrawler>();

            // Scenario 1: Fetching Seed URL returns a link to Page 1
            mockCrawler.Setup(c => c.FetchAsync(seedUrl)).ReturnsAsync(new CrawlResult(
                  seedUrl, "Home", "Welcome to LTU", Enumerable.Empty<IndexedTerm>(),
                  "text/html", Array.Empty<byte>(),
                  new List<string> { page1Url }, // Link found: triggers next step in recursion
                  HttpStatusCode.OK, 100
              ));

            // Scenario 2: Fetching Page 1 returns a link to Page 2
            mockCrawler.Setup(c => c.FetchAsync(page1Url)).ReturnsAsync(new CrawlResult(
                page1Url, "Page 1", "This is page 1", Enumerable.Empty<IndexedTerm>(),
                "text/html", Array.Empty<byte>(),
                new List<string> { page2Url }, // Link found: continues recursion
                HttpStatusCode.OK, 100
            ));

            // Scenario 3: Fetching Page 2 returns a link to the Final Page
            mockCrawler.Setup(c => c.FetchAsync(page2Url)).ReturnsAsync(new CrawlResult(
                page2Url, "Page 2", "This is page 2", Enumerable.Empty<IndexedTerm>(),
                "text/html", Array.Empty<byte>(),
                new List<string> { finalPageUrl }, // Link found: almost done
                HttpStatusCode.OK, 100
            ));

            // Scenario 4: Fetching Final Page returns no more links (Exit condition)
            mockCrawler.Setup(c => c.FetchAsync(finalPageUrl)).ReturnsAsync(new CrawlResult(
                finalPageUrl, "Final", "End of the line", Enumerable.Empty<IndexedTerm>(),
                "text/html", Array.Empty<byte>(),
                new List<string>(), // Empty list stops the recursion
                HttpStatusCode.OK, 100
            ));

            // Configure settings required for the crawler (including whitelist)
            var settings = new CrawlerSettings(
                userAgent: "TestBot",
                maxConcurrencyPerDomain: 5,
                minDelayMs: 0,
                retryIntervals: new[] { TimeSpan.FromMilliseconds(10) },
                seedUrls: new[] { "ltu.se" }
            );

            // --- DI Container Setup (In-Memory) ---
            // We build a minimal service provider to resolve dependencies like in the real app
            var services = new ServiceCollection();

            services.AddSingleton(mockCrawler.Object);
            services.AddSingleton(settings);

            // Register actual application services to be tested
            services.AddSingleton<ICrawlJobDispatcher, TplCrawlJobDispatcher>();
            services.AddTransient<IProcessCrawlJobUseCase, ProcessCrawlJobUseCase>();
            services.AddTransient<IIndexer, Indexer>();
            services.AddTransient<IndexingPipeline>();

            // Register a validator that allows the test URLs
            services.AddTransient<IDomainValidator>(sp => new DomainValidator(settings));

            // Use an In-Memory repository to capture results without a real database
            var fakeRepository = new InMemoryIndexRepository();
            services.AddSingleton<IIndexRepository>(fakeRepository);

            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<ICrawlJobDispatcher>();

            // ========================================================================
            // 2. ACT (Test Steps)
            // ========================================================================

            // Step 1: Queue the initial seed job to start the process
            var seedJob = new CrawlJob { Url = seedUrl, Status = CrawlJobStatus.Pending };
            await dispatcher.Enqueue(seedJob);

            // Step 2: Wait for the asynchronous recursive process to complete
            // Since the dispatcher runs in the background, we poll the repository 
            // until all 4 documents are indexed or we hit a timeout.
            int timeoutMs = 5000;
            int elapsed = 0;
            while (fakeRepository.Documents.Count < 4 && elapsed < timeoutMs)
            {
                await Task.Delay(100);
                elapsed += 100;
            }

            // ========================================================================
            // 3. ASSERT (Expected Results)
            // ========================================================================

            var storedDocs = fakeRepository.Documents;

            // Verify that all 4 pages were visited and stored. 
            // This proves the system successfully followed the links recursively.
            Assert.Equal(4, storedDocs.Count);

            Assert.Contains(storedDocs, d => d.Url == seedUrl);
            Assert.Contains(storedDocs, d => d.Url == page1Url);
            Assert.Contains(storedDocs, d => d.Url == page2Url);
            Assert.Contains(storedDocs, d => d.Url == finalPageUrl);
        }
    }

    /// <summary>
    /// A fake repository that stores results in a simple list for verification during tests.
    /// </summary>
    public class InMemoryIndexRepository : IIndexRepository
    {
        public List<IndexDocument> Documents { get; } = new();

        public Task SaveAsync(IndexDocument document)
        {
            lock (Documents) // Ensure thread-safety during concurrent crawling
            {
                if (!Documents.Any(d => d.Url == document.Url))
                {
                    Documents.Add(document);
                }
            }
            return Task.CompletedTask;
        }

        public Task AddDocumentAsync(string url, string title, string content)
        {
            var doc = new IndexDocument(Guid.NewGuid().ToString(), url, title);
            return SaveAsync(doc);
        }

        // Interface stubs not required for this specific test
        public Task AddDocumentAsync(string url, string title, List<string> words) => Task.CompletedTask;
        public Task<List<int>> GetPageIdsContainingTermAsync(string term) => Task.FromResult(new List<int>());
        public Task<List<Page>> GetPagesByIdsAsync(List<int> pageIds) => Task.FromResult(new List<Page>());
        public Task<List<IndexDocument>> SearchAsync(string query) => Task.FromResult(new List<IndexDocument>());
    }
}