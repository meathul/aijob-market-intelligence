using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Services.Salary;
using AiJobMarketIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.Application.Services.Processing;

public sealed class JobProcessingService : IJobProcessingService
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobProcessedRepository _jobProcessedRepository;
    private readonly ISalaryParserService _salaryParser;
    private readonly ILogger<JobProcessingService> _logger;

    public JobProcessingService(
        IJobRepository jobRepository,
        IJobProcessedRepository jobProcessedRepository,
        ISalaryParserService salaryParser,
        ILogger<JobProcessingService> logger)
    {
        _jobRepository = jobRepository;
        _jobProcessedRepository = jobProcessedRepository;
        _salaryParser = salaryParser;
        _logger = logger;
    }

    public async Task<int> ProcessPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _jobRepository.GetUnprocessedAsync();
        if (pending.Count == 0)
        {
            _logger.LogInformation("No unprocessed jobs found");
            return 0;
        }

        var processedCount = 0;

        foreach (var raw in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Salary parsing from raw field and/or description.
                var salary = _salaryParser.Parse(raw.SalaryRaw, raw.Description);

                // Experience level (heuristic) from description/title.
                var exp = InferExperienceLevel(raw.Title, raw.Description);

                var processed = new JobProcessed
                {
                    JobRawId = raw.Id,
                    SalaryMin = salary.SalaryMin,
                    SalaryMax = salary.SalaryMax,
                    Currency = salary.Currency,
                    SalaryPeriod = salary.SalaryPeriod,
                    ExperienceLevel = exp,
                    ProcessedAt = DateTime.UtcNow
                };

                await _jobProcessedRepository.UpsertByRawJobIdAsync(processed);

                raw.IsProcessed = true;
                await _jobRepository.UpdateAsync(raw);

                processedCount++;
            }
            catch (Exception ex)
            {
                // Keep raw job unprocessed so it can be retried later.
                _logger.LogError(ex, "Failed to process raw job {JobRawId}", raw.Id);
            }
        }

        await _jobProcessedRepository.SaveAsync();
        await _jobRepository.SaveAsync();

        _logger.LogInformation("Processed {Count} jobs", processedCount);
        return processedCount;
    }

    private static string? InferExperienceLevel(string? title, string? description)
    {
        var t = (title ?? string.Empty).ToLowerInvariant();
        var d = (description ?? string.Empty).ToLowerInvariant();
        var text = t + "\n" + d;

        if (text.Contains("intern")) return "Intern";
        if (text.Contains("junior") || text.Contains("jr.")) return "Junior";
        if (text.Contains("mid-level") || text.Contains("mid level")) return "Mid";
        if (text.Contains("senior") || text.Contains("sr.")) return "Senior";
        if (text.Contains("staff") || text.Contains("principal") || text.Contains("lead")) return "Lead";

        return null;
    }
}
