using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LTU.SearchEngine.BackgroundServices;

public class CrawlBackgroundService : BackgroundService
{
    private readonly ICrawlJobDispatcher _dispatcher;
    private readonly ILogger<CrawlBackgroundService> _logger;
    private readonly ICrawlerSettingsLoader _crawlerSettingsLoader;


    public CrawlBackgroundService(
        ICrawlJobDispatcher dispatcher, 
        ICrawlerSettingsLoader crawlerSettingsLoader,
        ILogger<CrawlBackgroundService> logger)
    {
        _dispatcher = dispatcher;
        _crawlerSettingsLoader = crawlerSettingsLoader;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CrawlBackgroundService is starting.");

        // Brief delay to ensure the application host has fully started (optional but recommended)
        await Task.Delay(1000, stoppingToken);

        try
        {
            // Create the initial Seed Job (Requirement FRQ-1001)
            var seedJob = new CrawlJob
            {
                Url = _crawlerSettingsLoader.Load().SeedUrls[0],
                // Set default values as required by the CrawlJob model
                Status = CrawlJobStatus.Pending,
                RetryCount = 0,
                NextAttempt = DateTime.UtcNow
            };

            _logger.LogInformation($"Enqueuing Seed Job: {_crawlerSettingsLoader.Load().SeedUrls[0]}");

            // Hand over the job to our TPL-based Dispatcher for processing
            await _dispatcher.Enqueue(seedJob);
            await _dispatcher.Start(stoppingToken);

            _logger.LogInformation("Seed Job enqueued successfully.");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "CrawlerBackgroundService is shutting down.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue seed job.");
        }
    }
}
