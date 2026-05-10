namespace AiJobMarketIntelligence.Application.Services.Processing;

public interface IJobProcessingService
{
    /// <summary>
    /// Processes pending raw jobs (salary parsing + heuristics).
    /// Returns number of jobs processed.
    /// </summary>
    Task<int> ProcessPendingJobsAsync(CancellationToken cancellationToken = default);
}
