using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Application.Interfaces.Repositories;

public interface IJobRepository
{
    Task<JobRaw?> GetByIdAsync(int id);
    Task<JobRaw?> GetByUrlAsync(string url);

    Task<List<JobRaw>> GetAllAsync(int pageNumber, int pageSize);
    Task<int> CountAsync();

    Task<List<JobRaw>> SearchAsync(string keyword, string? location, int pageNumber, int pageSize);

    Task<List<JobRaw>> GetUnprocessedAsync();

    Task AddAsync(JobRaw job);
    Task AddRangeAsync(IEnumerable<JobRaw> jobs);
    Task UpdateAsync(JobRaw job);

    Task<bool> ExistsByUrlAsync(string url);

    Task SaveAsync();
}
