using AiJobMarketIntelligence.Application.DTOs;

namespace AiJobMarketIntelligence.Application.Interfaces.Services;

public interface IJobQueryService
{
    Task<JobSearchResultDto> GetJobsAsync(int pageNumber, int pageSize);
    Task<JobRawDto?> GetJobByIdAsync(int id);
    Task<JobSearchResultDto> SearchJobsAsync(string? keyword, string? location, int pageNumber, int pageSize);
}
