using Microsoft.EntityFrameworkCore;
using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Infrastructure.Data;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;

namespace AiJobMarketIntelligence.Infrastructure.Repositories;

// Interface moved to Application layer: AiJobMarketIntelligence.Application.Interfaces.Repositories.IJobRepository

public class JobRepository : IJobRepository
{
    private readonly AiJobContext _context;

    public JobRepository(AiJobContext context)
    {
        _context = context;
    }

    public async Task<JobRaw?> GetByIdAsync(int id)
    {
        return await _context.JobsRaw
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<JobRaw?> GetByUrlAsync(string url)
    {
        return await _context.JobsRaw
            .FirstOrDefaultAsync(j => j.Url == url);
    }

    public async Task<List<JobRaw>> GetAllAsync(int pageNumber, int pageSize)
    {
        return await _context.JobsRaw
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .OrderByDescending(j => j.PostedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<JobRaw>> SearchAsync(string keyword, string? location, int pageNumber, int pageSize)
    {
        var query = _context.JobsRaw.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(j => 
                j.Title.Contains(keyword) || 
                j.Description.Contains(keyword) ||
                j.Company.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(j => j.Location.Contains(location));
        }

        return await query
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .OrderByDescending(j => j.PostedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<JobRaw>> GetUnprocessedAsync()
    {
        return await _context.JobsRaw
            .Where(j => !j.IsProcessed)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(JobRaw job)
    {
        await _context.JobsRaw.AddAsync(job);
    }

    public async Task AddRangeAsync(IEnumerable<JobRaw> jobs)
    {
        await _context.JobsRaw.AddRangeAsync(jobs);
    }

    public async Task UpdateAsync(JobRaw job)
    {
        _context.JobsRaw.Update(job);
        await Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.JobsRaw.CountAsync();
    }

    public async Task<bool> ExistsByUrlAsync(string url)
    {
        return await _context.JobsRaw.AnyAsync(j => j.Url == url);
    }
}
