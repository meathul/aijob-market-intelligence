using AiJobMarketIntelligence.Application.Services.Processing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.Worker;

/// <summary>
/// Background worker that processes raw jobs into structured processed jobs.
/// </summary>
public sealed class JobProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobProcessingWorker> _logger;

    public JobProcessingWorker(IServiceProvider serviceProvider, ILogger<JobProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Processing Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processingService = scope.ServiceProvider.GetRequiredService<IJobProcessingService>();

                var processed = await processingService.ProcessPendingJobsAsync(stoppingToken);
                _logger.LogInformation("Job processing run completed. Jobs processed: {Count}", processed);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job processing worker loop error");
            }

            // Run every 15 minutes by default
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }

        _logger.LogInformation("Job Processing Worker stopping");
    }
}
