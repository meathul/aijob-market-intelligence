using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiJobMarketIntelligence.Infrastructure.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly AiJobContext _context;

    public SkillRepository(AiJobContext context)
    {
        _context = context;
    }

    public async Task<Skill?> GetByIdAsync(int id)
    {
        return await _context.Skills.FindAsync(id);
    }

    public async Task<Skill?> GetByNameAsync(string name)
    {
        return await _context.Skills.FirstOrDefaultAsync(s => s.SkillName.ToLower() == name.ToLower());
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

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Skills.AnyAsync(s => s.SkillName.ToLower() == name.ToLower());
    }

    public async Task<List<string>> GetSkillsByJobRawIdAsync(int jobRawId)
    {
        return await _context.JobSkills
            .Where(js => js.JobRawId == jobRawId)
            .Select(js => js.SkillName)
            .ToListAsync();
    }

    public async Task AddJobSkillAsync(int jobRawId, string skillName)
    {
        // Check if the skill already exists for this job
        var exists = await _context.JobSkills
            .AnyAsync(js => js.JobRawId == jobRawId && js.SkillName.ToLower() == skillName.ToLower());

        if (!exists)
        {
            var jobSkill = new JobSkill
            {
                JobRawId = jobRawId,
                SkillName = skillName
            };
            await _context.JobSkills.AddAsync(jobSkill);
        }
    }

    public async Task<List<JobSkill>> GetJobSkillsByJobRawIdAsync(int jobRawId)
    {
        return await _context.JobSkills
            .Where(js => js.JobRawId == jobRawId)
            .ToListAsync();
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}
