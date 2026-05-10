using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Application.Interfaces.Repositories;

public interface IJobProcessedRepository
{
    Task<JobProcessed?> GetByRawJobIdAsync(int jobRawId);
    Task UpsertByRawJobIdAsync(JobProcessed processed);
    Task SaveAsync();
}
