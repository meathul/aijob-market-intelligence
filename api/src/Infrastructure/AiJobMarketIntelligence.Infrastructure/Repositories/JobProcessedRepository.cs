using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiJobMarketIntelligence.Infrastructure.Repositories;

public sealed class JobProcessedRepository : IJobProcessedRepository
{
    private readonly AiJobContext _context;

    public JobProcessedRepository(AiJobContext context)
    {
        _context = context;
    }

    public Task<JobProcessed?> GetByRawJobIdAsync(int jobRawId)
    {
        return _context.JobsProcessed.FirstOrDefaultAsync(p => p.JobRawId == jobRawId);
    }

    public async Task UpsertByRawJobIdAsync(JobProcessed processed)
    {
        var existing = await GetByRawJobIdAsync(processed.JobRawId);
        if (existing is null)
        {
            await _context.JobsProcessed.AddAsync(processed);
            return;
        }

        existing.SalaryMin = processed.SalaryMin;
        existing.SalaryMax = processed.SalaryMax;
        existing.Currency = processed.Currency;
        existing.SalaryPeriod = processed.SalaryPeriod;
        existing.ExperienceLevel = processed.ExperienceLevel;
        existing.ProcessedAt = processed.ProcessedAt;
    }

    public Task SaveAsync() => _context.SaveChangesAsync();
}
