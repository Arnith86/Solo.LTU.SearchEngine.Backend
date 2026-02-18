using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LTU.SearchEngine.BackgroundServices
{
    public class CrawlBackgroundService : BackgroundService
    {
        private readonly ICrawlJobDispatcher _dispatcher;
        private readonly ILogger<CrawlBackgroundService> _logger;

        // Future improvement: Inject IOptions<CrawlerSettings> to fetch the Seed URL from config instead of a constant
        private const string SeedUrl = "https://www.ltu.se";

        public CrawlBackgroundService(ICrawlJobDispatcher dispatcher, ILogger<CrawlBackgroundService> logger)
        {
            _dispatcher = dispatcher;
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
                    Url = SeedUrl,
                    // Set default values as required by the CrawlJob model
                    Status = CrawlJobStatus.Pending,
                    RetryCount = 0
                };

                _logger.LogInformation($"Enqueuing Seed Job: {SeedUrl}");

                // Hand over the job to our TPL-based Dispatcher for processing
                await _dispatcher.Enqueue(seedJob);

                _logger.LogInformation("Seed Job enqueued successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue seed job.");
            }

            // The BackgroundService stays alive, even though ExecuteAsync has finished its initialization.
            // To implement recurring tasks (e.g., scheduled re-crawling), a 
            // `while (!stoppingToken.IsCancellationRequested)` loop could be added here.
        }
    }
}