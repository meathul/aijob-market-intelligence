using Microsoft.Extensions.DependencyInjection;
using AiJobMarketIntelligence.Application.Services;

namespace AiJobMarketIntelligence.Worker;

/// <summary>
/// Background worker service that runs job ingestion at regular intervals.
/// Executes every 30 minutes by default (configurable via settings).
/// </summary>
public class JobIngestionWorker : BackgroundService
{
    private readonly ILogger<JobIngestionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval;

    public JobIngestionWorker(ILogger<JobIngestionWorker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Get interval from configuration, default to 30 minutes
        var intervalMinutes = configuration.GetValue<int>("JobIngestion:IntervalMinutes", 30);
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Ingestion Worker starting. Interval: {IntervalMinutes} minutes", _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformJobIngestionAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in job ingestion worker");
            }

            // Wait for the configured interval before next execution
            _logger.LogInformation("Next job ingestion scheduled in {IntervalMinutes} minutes", _interval.TotalMinutes);
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Job Ingestion Worker stopping");
    }

    /// <summary>
    /// Performs the actual job ingestion work.
    /// </summary>
    private async Task PerformJobIngestionAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting job ingestion at {Time}", DateTime.UtcNow);

        using (var scope = _serviceProvider.CreateScope())
        {
            var jobIngestionService = scope.ServiceProvider.GetRequiredService<IJobIngestionService>();
            var jobsAdded = await jobIngestionService.IngestJobsAsync();

            if (jobsAdded > 0)
            {
                _logger.LogInformation("Successfully ingested {JobCount} new jobs", jobsAdded);
            }
            else
            {
                _logger.LogDebug("No new jobs to ingest");
            }
        }
    }
}

