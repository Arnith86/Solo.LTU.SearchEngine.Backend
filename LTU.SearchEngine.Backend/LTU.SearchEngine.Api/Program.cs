using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Configuration;
using LTU.SearchEngine.Infrastructure.Crawling;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Infrastructure.Indexing.Normalization;
using LTU.SearchEngine.Infrastructure.Indexing.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LTU.SearchEngine.Backend.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ========================================================================
        // 1. Configuration & Settings
        // ========================================================================
        // Loads crawler configuration (seed URLs, delays, concurrency limits) from JSON.
        builder.Services.AddSingleton<ICrawlerSettingsLoader, JsonCrawlerSettingsLoader>();
        builder.Services.AddSingleton(serviceProvider =>
        {
            var loader = serviceProvider.GetRequiredService<ICrawlerSettingsLoader>();
            return loader.Load(); // Returns the immutable CrawlerSettings object
        });

        // ========================================================================
        // 2. Database & Persistence
        // ========================================================================
        // Uses DbContextFactory to handle concurrency safely within TPL (Thread Parallel Library).
        builder.Services.AddDbContextFactory<SearchDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Repository pattern for abstracting database operations.
        builder.Services.AddTransient<IIndexRepository, SqlIndexRepository>();

        // ========================================================================
        // 3. Infrastructure & Core Services
        // ========================================================================
        // HTTP Client for fetching web pages.
        builder.Services.AddHttpClient();

        // Semaphore for handling global concurrency/throttling if needed.
        builder.Services.AddSingleton<SemaphoreProvider>();

        // HTML Parsing strategy (e.g., using HtmlAgilityPack).
        builder.Services.AddTransient<IHtmlParser, HapHtmlParser>();

        // Domain Validator (White-listing logic) and Indexing Pipeline (Text Normalization).
        builder.Services.AddTransient<IDomainValidator, DomainValidator>();
        builder.Services.AddTransient<ITextFilter, LuceneAnalyzerFilter>();
        builder.Services.AddTransient<ITextNormalizer, TextNormalizer>();
        builder.Services.AddTransient<IndexingPipeline>();

        // Services that orchestrate the actual work (Fetching and Indexing).
        builder.Services.AddTransient<ICrawler, Crawler>();
        builder.Services.AddTransient<IIndexer, Indexer>();

        // ========================================================================
        // 4. Application Logic (Use Cases)
        // ========================================================================
        // The main unit of work: Orchestrates Crawling -> Validating -> Indexing for a single job.
        builder.Services.AddTransient<IProcessCrawlJobUseCase, ProcessCrawlJobUseCase>();

        // ========================================================================
        // 5. Background Services & TPL Engine
        // ========================================================================
        // The Dispatcher manages the TPL Dataflow pipeline (Queue).
        builder.Services.AddSingleton<ICrawlJobDispatcher, TplCrawlJobDispatcher>();

        // The Hosted Service that starts the "Seed Job" on application startup (FRQ-1001).
        builder.Services.AddHostedService<CrawlBackgroundService>();

        // ========================================================================
        // 6. API & Presentation
        // ========================================================================
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
