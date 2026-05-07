using Microsoft.EntityFrameworkCore;
using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Infrastructure.Data;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;

namespace AiJobMarketIntelligence.Infrastructure.Repositories;

/// <summary>
/// Repository for Skill entities.
/// </summary>
public class SkillRepository : ISkillRepository
{
    private readonly AiJobContext _context;

    public SkillRepository(AiJobContext context)
    {
        _context = context;
    }

    public async Task<Skill?> GetByIdAsync(int id)
    {
        return await _context.Skills
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Skill?> GetByNameAsync(string name)
    {
        return await _context.Skills
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<List<Skill>> GetAllAsync()
    {
        return await _context.Skills.ToListAsync();
    }

    public async Task AddAsync(Skill skill)
    {
        await _context.Skills.AddAsync(skill);
    }

    public async Task AddRangeAsync(IEnumerable<Skill> skills)
    {
        await _context.Skills.AddRangeAsync(skills);
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Skills.AnyAsync(s => s.Name == name);
    }
}
