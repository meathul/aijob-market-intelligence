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
        var query = ApplySearch(_context.JobsRaw.AsQueryable(), keyword, location);

        return await query
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .OrderByDescending(j => j.PostedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountSearchAsync(string? keyword, string? location)
    {
        var query = ApplySearch(_context.JobsRaw.AsQueryable(), keyword, location);
        return await query.CountAsync();
    }

    public async Task<List<JobRaw>> GetUnprocessedAsync()
    {
        return await _context.JobsRaw
            .Where(j => !j.IsProcessed)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<JobRaw>> GetProcessedAsync(int pageNumber, int pageSize)
    {
        return await _context.JobsRaw
            .Where(j => j.IsProcessed)
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .OrderByDescending(j => j.PostedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountProcessedAsync()
    {
        return await _context.JobsRaw.CountAsync(j => j.IsProcessed);
    }

    public async Task<List<(string Skill, int Count)>> GetTopSkillsAsync(int take)
    {
        if (take <= 0) return new();

        var rows = await _context.JobSkills
            .AsNoTracking()
            .Include(js => js.Skill)
            .GroupBy(js => js.Skill.Name)
            .Select(g => new { Skill = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Skill)
            .Take(take)
            .ToListAsync();

        return rows.Select(r => (r.Skill, r.Count)).ToList();
    }

    public async Task<(int? Min, int? Max, double? Avg)> GetSalaryStatsAsync(
        string? currency,
        string? location,
        string? experienceLevel,
        int? postedWithinDays)
    {
        var query = _context.JobsProcessed
            .AsNoTracking()
            .Include(p => p.JobRaw)
            .Where(p => p.SalaryMin != null || p.SalaryMax != null);

        if (!string.IsNullOrWhiteSpace(currency))
            query = query.Where(p => p.Currency == currency);

        if (!string.IsNullOrWhiteSpace(experienceLevel))
            query = query.Where(p => p.ExperienceLevel == experienceLevel);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(p => p.JobRaw.Location.Contains(location));

        if (postedWithinDays is > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-postedWithinDays.Value);
            query = query.Where(p => p.JobRaw.PostedDate >= cutoff);
        }

        // Define a single "representative" salary per row (average of min/max when both exist).
        var salaryQuery = query.Select(p =>
            p.SalaryMin != null && p.SalaryMax != null
                ? (p.SalaryMin.Value + p.SalaryMax.Value) / 2.0
                : (p.SalaryMin ?? p.SalaryMax)!.Value);

        var count = await salaryQuery.CountAsync();
        if (count == 0) return (null, null, null);

        var min = await salaryQuery.MinAsync();
        var max = await salaryQuery.MaxAsync();
        var avg = await salaryQuery.AverageAsync();

        return ((int)min, (int)max, avg);
    }

    public async Task<List<(DateOnly Day, int Count)>> GetIngestionDailyCountsAsync(int days)
    {
        if (days <= 0) return new();
        if (days > 3650) days = 3650;

        var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);

        // Group by date portion of CreatedAt.
        var rows = await _context.JobsRaw
            .AsNoTracking()
            .Where(j => j.CreatedAt >= cutoff)
            .GroupBy(j => new { j.CreatedAt.Year, j.CreatedAt.Month, j.CreatedAt.Day })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .ToListAsync();

        return rows
            .Select(r => (new DateOnly(r.Year, r.Month, r.Day), r.Count))
            .ToList();
    }

    public async Task<List<(string Source, int Count)>> GetCountsBySourceAsync(int take)
    {
        if (take <= 0) return new();

        var rows = await _context.JobsRaw
            .AsNoTracking()
            .GroupBy(j => j.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Source)
            .Take(take)
            .ToListAsync();

        return rows.Select(r => (r.Source, r.Count)).ToList();
    }

    public async Task<List<(string Location, int Count)>> GetCountsByLocationAsync(int take)
    {
        if (take <= 0) return new();

        var rows = await _context.JobsRaw
            .AsNoTracking()
            .GroupBy(j => j.Location)
            .Select(g => new { Location = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Location)
            .Take(take)
            .ToListAsync();

        return rows.Select(r => (r.Location, r.Count)).ToList();
    }

    public async Task<List<(string ExperienceLevel, int Count)>> GetCountsByExperienceLevelAsync(int take)
    {
        if (take <= 0) return new();

        var rows = await _context.JobsProcessed
            .AsNoTracking()
            .Where(p => p.ExperienceLevel != null && p.ExperienceLevel != "")
            .GroupBy(p => p.ExperienceLevel!)
            .Select(g => new { ExperienceLevel = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.ExperienceLevel)
            .Take(take)
            .ToListAsync();

        return rows.Select(r => (r.ExperienceLevel, r.Count)).ToList();
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

    private static IQueryable<JobRaw> ApplySearch(IQueryable<JobRaw> query, string? keyword, string? location)
    {
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

        return query;
    }
}
