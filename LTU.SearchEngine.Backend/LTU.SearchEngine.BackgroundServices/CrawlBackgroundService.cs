using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.BackgroundServices
{
    public class CrawlBackgroundService : BackgroundService
    {
        private readonly ICrawlJobDispatcher _dispatcher;
        private readonly ILogger<CrawlBackgroundService> _logger;

        // Här kan du senare injicera IOptions<CrawlerSettings> för att hämta seed-URL från config
        private const string SeedUrl = "https://www.ltu.se";

        public CrawlBackgroundService(ICrawlJobDispatcher dispatcher, ILogger<CrawlBackgroundService> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CrawlBackgroundService is starting.");

            // Vänta lite så att applikationen hinner starta upp helt (valfritt men rekommenderat)
            await Task.Delay(1000, stoppingToken);

            try
            {
                // Skapa Seed Jobbet (FRQ-1001)
                var seedJob = new CrawlJob
                {
                    Url = SeedUrl,
                    // Sätt defaultvärden om din CrawlJob-modell kräver det
                    Status = CrawlJobStatus.Pending,
                    RetryCount = 0
                };

                _logger.LogInformation($"Enqueuing Seed Job: {SeedUrl}");

                // Skicka jobbet till vår TPL Dispatcher
                await _dispatcher.Enqueue(seedJob);

                _logger.LogInformation("Seed Job enqueued successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue seed job.");
            }

            // BackgroundService fortsätter leva, men ExecuteAsync är klar med sin initiering.
            // Om du vill att den ska göra återkommande saker (t.ex. schemalagd om-crawling)
            // kan du lägga en `while (!stoppingToken.IsCancellationRequested)` loop här.
        }
    }
}
