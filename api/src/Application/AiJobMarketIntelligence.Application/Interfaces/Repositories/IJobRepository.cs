using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Application.Interfaces.Repositories;

public interface IJobRepository
{
    Task<JobRaw?> GetByIdAsync(int id);
    Task<JobRaw?> GetByUrlAsync(string url);

    Task<List<JobRaw>> GetAllAsync(int pageNumber, int pageSize);
    Task<int> CountAsync();

    Task<List<JobRaw>> SearchAsync(string keyword, string? location, int pageNumber, int pageSize);
    Task<int> CountSearchAsync(string? keyword, string? location);

    Task<List<JobRaw>> GetUnprocessedAsync();
    Task<List<JobRaw>> GetProcessedAsync(int pageNumber, int pageSize);
    Task<int> CountProcessedAsync();

    Task<List<(string Skill, int Count)>> GetTopSkillsAsync(int take);

    // Analytics
    Task<(int? Min, int? Max, double? Avg)> GetSalaryStatsAsync(
        string? currency,
        string? location,
        string? experienceLevel,
        int? postedWithinDays);

    Task<List<(DateOnly Day, int Count)>> GetIngestionDailyCountsAsync(int days);

    Task<List<(string Source, int Count)>> GetCountsBySourceAsync(int take);

    Task<List<(string Location, int Count)>> GetCountsByLocationAsync(int take);

    Task<List<(string ExperienceLevel, int Count)>> GetCountsByExperienceLevelAsync(int take);

    Task AddAsync(JobRaw job);
    Task AddRangeAsync(IEnumerable<JobRaw> jobs);
    Task UpdateAsync(JobRaw job);

    Task<bool> ExistsByUrlAsync(string url);

    Task SaveAsync();
}
