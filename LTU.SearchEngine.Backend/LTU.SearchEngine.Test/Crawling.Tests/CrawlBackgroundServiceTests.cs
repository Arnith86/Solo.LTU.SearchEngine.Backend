using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Crawling;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace LTU.SearchEngine.Test.Crawling.Tests
{
    public class CrawlBackgroundServiceTests
    {
        [Fact]
        public async Task TC_FRQ_1001_Verify_Crawler_Visits_All_Reachable_Pages_Recursively()
        {
            // --- ARRANGE ---
            var seedUrl = "https://www.ltu.se";
            var page1Url = "https://www.ltu.se/page1";
            var page2Url = "https://www.ltu.se/page2";
            var finalPageUrl = "https://www.ltu.se/final";

            var dummyTerms = new List<IndexedTerm> { new IndexedTerm("test", TermSource.Title) };

            var mockCrawler = new Mock<ICrawler>();

            // VIKTIGT: Här mappar vi specifika URL:er till specifika svar. 
            // Nu vet Moq skillnad på startsidan och undersidorna.
            mockCrawler.Setup(c => c.FetchAsync(seedUrl)).ReturnsAsync(new CrawlResult(
                seedUrl, "Home", "Welcome", dummyTerms, "text/html", Array.Empty<byte>(),
                new List<string> { page1Url }, HttpStatusCode.OK, 100));

            mockCrawler.Setup(c => c.FetchAsync(page1Url)).ReturnsAsync(new CrawlResult(
                page1Url, "Page 1", "Content 1", dummyTerms, "text/html", Array.Empty<byte>(),
                new List<string> { page2Url }, HttpStatusCode.OK, 100));

            mockCrawler.Setup(c => c.FetchAsync(page2Url)).ReturnsAsync(new CrawlResult(
                page2Url, "Page 2", "Content 2", dummyTerms, "text/html", Array.Empty<byte>(),
                new List<string> { finalPageUrl }, HttpStatusCode.OK, 100));

            mockCrawler.Setup(c => c.FetchAsync(finalPageUrl)).ReturnsAsync(new CrawlResult(
                finalPageUrl, "Final", "End", dummyTerms, "text/html", Array.Empty<byte>(),
                new List<string>(), HttpStatusCode.OK, 100));

            var settings = new CrawlerSettings(
                userAgent: "TestBot",
                maxConcurrencyPerDomain: 5,
                minDelayMs: 10, // Ge trådarna tid att andas
                retryIntervals: new[] { TimeSpan.FromMilliseconds(10) },
                seedUrls: new[] { "www.ltu.se", "ltu.se" }
            );

            var services = new ServiceCollection();
            services.AddSingleton(mockCrawler.Object);
            services.AddSingleton(settings);
            services.AddSingleton<SemaphoreProvider>();
            services.AddLogging();

            services.AddSingleton<ICrawlJobDispatcher, TplCrawlJobDispatcher>();
            services.AddTransient<IProcessCrawlJobUseCase, ProcessCrawlJobUseCase>();
            services.AddTransient<IIndexer, Indexer>();

            
            var mockNormalizer = new Mock<ITextNormalizer<string>>();

            var mockPipeline = new Mock<IIndexingPipeline>();


            mockPipeline.Setup(p => p.Transform(It.IsAny<CrawlResult>()))
                .Returns((CrawlResult r) => new IndexDocument(Guid.NewGuid().ToString(), r.Url, r.Title));

            services.AddSingleton(mockPipeline.Object);

            services.AddTransient<IDomainValidator>(sp => new DomainValidator(settings));

            var fakeRepository = new InMemoryIndexRepository();
            services.AddSingleton<IIndexRepository>(fakeRepository);

            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<ICrawlJobDispatcher>();

            // --- ACT ---
            using var cts = new CancellationTokenSource();
            var dispatcherTask = dispatcher.Start(cts.Token);

            // Ge pipelinen en chans att koppla ihop sig
            await Task.Delay(100);

            var seedJob = new CrawlJob { Url = seedUrl, Status = CrawlJobStatus.Pending };
            await dispatcher.Enqueue(seedJob);

            // Polling: Vänta tills alla 4 är hittade eller vi når timeout
            int timeoutMs = 5000;
            int elapsed = 0;
            while (fakeRepository.Documents.Count < 4 && elapsed < timeoutMs)
            {
                await Task.Delay(200);
                elapsed += 200;
                if (dispatcherTask.IsFaulted) await dispatcherTask;
            }

            cts.Cancel();

            // --- ASSERT ---
            Assert.Equal(4, fakeRepository.Documents.Count);
            Assert.Contains(fakeRepository.Documents, d => d.Url == seedUrl);
            Assert.Contains(fakeRepository.Documents, d => d.Url == finalPageUrl);
        }
    }

    public class InMemoryIndexRepository : IIndexRepository
    {
        public List<IndexDocument> Documents { get; } = new();

        public Task SaveAsync(IndexDocument document)
        {
            lock (Documents)
            {
                if (!Documents.Any(d => d.Url == document.Url))
                    Documents.Add(document);
            }
            return Task.CompletedTask;
        }

        public Task AddDocumentAsync(string url, string title, List<string> words)
            => SaveAsync(new IndexDocument(Guid.NewGuid().ToString(), url, title));

		public Task<List<IndexDocument>> SearchAsync(string query) =>
			Task.FromResult(new List<IndexDocument>());

		public Task AddDocumentAsync(string url, string title, string content)
            => SaveAsync(new IndexDocument(Guid.NewGuid().ToString(), url, title));

        public Task<HashSet<int>> GetDocumentIdsForTermAsync(string term) => 
            Task.FromResult(new HashSet<int>());
        public Task<List<Page>> GetDocumentsByIdAsync(List<int> pageIds) => 
            Task.FromResult(new List<Page>());
        public Task<HashSet<int>> GetDocumentIdsForPhraseAsync(PhraseNode<HashSet<int>> phrase) =>
            Task.FromResult(new HashSet<int>());
	}
}