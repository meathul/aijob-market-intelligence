using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Application.Services.Providers;

/// <summary>
/// Interface for external job data providers.
/// Implementations fetch job listings from various external APIs.
/// </summary>
public interface IJobProvider
{
    /// <summary>
    /// Fetches jobs from the external provider.
    /// </summary>
    Task<List<JobRaw>> FetchJobsAsync();
}
