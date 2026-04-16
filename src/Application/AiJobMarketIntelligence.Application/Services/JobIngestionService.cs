using Microsoft.Extensions.Logging;
using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Infrastructure.Repositories;
using AiJobMarketIntelligence.Application.Services.Providers;

namespace AiJobMarketIntelligence.Application.Services;

/// <summary>
/// Service responsible for ingesting jobs from external providers into the database.
/// Handles deduplication, data validation, and persistence.
/// </summary>
public interface IJobIngestionService
{
    /// <summary>
    /// Fetches jobs from configured providers and saves new ones to the database.
    /// Returns the count of jobs added.
    /// </summary>
    Task<int> IngestJobsAsync();
}

public class JobIngestionService : IJobIngestionService
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobProvider _jobProvider;
    private readonly ILogger<JobIngestionService> _logger;

    public JobIngestionService(
        IJobRepository jobRepository,
        IJobProvider jobProvider,
        ILogger<JobIngestionService> logger)
    {
        _jobRepository = jobRepository;
        _jobProvider = jobProvider;
        _logger = logger;
    }

    public async Task<int> IngestJobsAsync()
    {
        try
        {
            _logger.LogInformation("Starting job ingestion process");

            // Fetch jobs from provider
            var fetchedJobs = await _jobProvider.FetchJobsAsync();
            _logger.LogInformation("Fetched {Count} jobs from provider", fetchedJobs.Count);

            if (!fetchedJobs.Any())
            {
                _logger.LogWarning("No jobs received from provider");
                return 0;
            }

            // Deduplicate: Filter out jobs that already exist in the database
            var newJobs = new List<JobRaw>();

            foreach (var job in fetchedJobs)
            {
                // Validate job data
                if (!ValidateJob(job))
                {
                    _logger.LogWarning("Skipping invalid job: {Title} from {Company}", job.Title, job.Company);
                    continue;
                }

                // Check if job already exists
                var exists = await _jobRepository.ExistsByUrlAsync(job.Url);
                if (!exists)
                {
                    newJobs.Add(job);
                }
                else
                {
                    _logger.LogDebug("Job already exists: {Url}", job.Url);
                }
            }

            if (!newJobs.Any())
            {
                _logger.LogInformation("No new jobs to add after deduplication");
                return 0;
            }

            // Save new jobs
            await _jobRepository.AddRangeAsync(newJobs);
            await _jobRepository.SaveAsync();

            _logger.LogInformation("Successfully ingested {Count} new jobs", newJobs.Count);
            return newJobs.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job ingestion process");
            throw;
        }
    }

    /// <summary>
    /// Validates that a job has all required fields and valid data.
    /// </summary>
    private static bool ValidateJob(JobRaw job)
    {
        if (string.IsNullOrWhiteSpace(job.Title))
            return false;

        if (string.IsNullOrWhiteSpace(job.Company))
            return false;

        if (string.IsNullOrWhiteSpace(job.Location))
            return false;

        if (string.IsNullOrWhiteSpace(job.Description))
            return false;

        if (string.IsNullOrWhiteSpace(job.Url))
            return false;

        if (string.IsNullOrWhiteSpace(job.Source))
            return false;

        if (job.PostedDate == default(DateTime))
            return false;

        return true;
    }
}
