using LTU.SearchEngine.Api.ExtensionsUseExceptionHandler;
using LTU.SearchEngine.Application;
using LTU.SearchEngine.Application.QueryParsing;
using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.HelperClasses;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using LTU.SearchEngine.Backend.Core.TextNormalization;
using LTU.SearchEngine.BackgroundServices;
using LTU.SearchEngine.Infrastructure;
using LTU.SearchEngine.Infrastructure.Configuration;
using LTU.SearchEngine.Infrastructure.Configurations;
using LTU.SearchEngine.Infrastructure.Crawling;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Indexing;
using LTU.SearchEngine.Infrastructure.Repositories;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Backend.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // ========================================================================
        // 1. Configuration & Settings
        // ========================================================================
        // Loads "hotswappable" crawler configuration (seed URLs, delays, concurrency limits) from JSON.
        builder.Services.AddTransient<ICrawlerSettingsLoader, JsonCrawlerSettingsLoader>();
        builder.Services.Configure<CrawlerSettingsDTO>(builder.Configuration.GetSection("CrawlerSettings"));

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
        builder.Services.AddSingleton<IContentHasher, ContentHasher>();
        builder.Services.AddSingleton<SemaphoreProvider>();

        // HTML Parsing strategy (e.g., using HtmlAgilityPack).
        builder.Services.AddTransient<IHtmlParser, HapHtmlParser>();

        // Domain Validator (White-listing logic) and Indexing Pipeline (Text Normalization).
        builder.Services.AddTransient<IDomainValidator, DomainValidator>();
        builder.Services.AddTransient<IRobotsHandler, RobotsHandler>();
        builder.Services.AddSingleton<LuceneAnalyzerStrategy>();
        builder.Services.AddSingleton<ILanguageAnalyzerRegistry, LanguageAnalyzerRegistry>();
        builder.Services.AddTransient<IHtmlLanguageCodeConverter, HtmlLanguageCodeConverter>();
        builder.Services.AddTransient<ITextFilter, LuceneAnalyzerFilter>();
        builder.Services.AddTransient<ITextNormalizer<string>, TextNormalizer>();
        builder.Services.AddTransient<IIndexingPipeline, IndexingPipeline>();

        // Services that orchestrate the actual work (Fetching and Indexing).
        builder.Services.AddTransient<ICrawler, Crawler>();
        builder.Services.AddTransient<IIndexer, Indexer>();


        builder.Services.AddScoped<IQueryService, QueryService>();

        builder.Services.AddScoped<IServiceManager, ServiceManager>();

        // ========================================================================
        // 4. Search & Query Logic (Core)
        // ========================================================================
        // Tokenizer and TreeBuilder used by the Parser to transform strings into ASTs.
        builder.Services.AddTransient<IQuerySyntaxHelper, QuerySyntaxHelper>();

		builder.Services.AddTransient<
            IStringTokenizer<ExtractedQueryToken, IgnoredTermsDTO>, 
            QueryStringTokenizer>();
		
        builder.Services.AddTransient<
            ITreeBuilder<HashSet<int>, ExtractedQueryToken>, 
            AbstractSyntaxTreeBuilder<HashSet<int>>>();

        builder.Services.AddTransient<
            IShuntingYardParser<ExtractedQueryToken>, 
            SearchQueryShuntingYardParser>();

		// The Parser that orchestrates the transformation.
		builder.Services.AddTransient<IQueryParser, QueryParser>();

		// Visitor that evaluates the resulting tree against the database.
		builder.Services.AddTransient<IQueryVisitor<HashSet<int>>, QueryEvaluatorVisitor>();

		// ========================================================================
		// 5. Application Logic (Use Cases)
		// ========================================================================
		// The main unit of work: Orchestrates Crawling -> Validating -> Indexing for a single job.
		builder.Services.AddTransient<IProcessCrawlJobUseCase, ProcessCrawlJobUseCase>();

        // ========================================================================
        // 6. Background Services & TPL Engine
        // ========================================================================
        // The Dispatcher manages the TPL Dataflow pipeline (Queue).
        builder.Services.AddSingleton<ICrawlJobDispatcher, TplCrawlJobDispatcher>();

        // The Hosted Service that starts the "Seed Job" on application startup (FRQ-1001).
        builder.Services.AddHostedService<CrawlBackgroundService>();

        // ========================================================================
        // 7. API & Presentation
        // ========================================================================
        builder.Services.AddControllers();
        
        // builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("PublicApi",
                policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
        });

        builder.Services.AddRateLimiter(options =>
        {
           options.AddFixedWindowLimiter("PublicSearchPolicy", opt =>
           {
                opt.Window = TimeSpan.FromSeconds(10);
                opt.PermitLimit = 5;   
                opt.QueueLimit = 4;
           });
        });

        var app = builder.Build();

		app.ConfigureExceptionHandler();
        app.UseCors("PublicApi");
        app.UseRateLimiter();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
        {
            // app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
