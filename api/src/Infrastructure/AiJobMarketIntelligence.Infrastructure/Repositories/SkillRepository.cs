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
    private const int SkillNameMaxLen = 255;

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
        return await _context.Skills.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
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
        return await _context.Skills.AnyAsync(s => s.Name.ToLower() == name.ToLower());
    }

    public async Task<List<string>> GetSkillsByJobRawIdAsync(int jobRawId)
    {
        return await _context.JobSkills
            .Where(js => js.JobRawId == jobRawId)
            .Select(js => js.Skill.Name)
            .ToListAsync();
    }

    public async Task AddJobSkillAsync(int jobRawId, string skillName)
    {
        skillName = NormalizeSkillName(skillName);
        if (string.IsNullOrWhiteSpace(skillName))
            return;

        // Check if skill exists
        var skill = await GetByNameAsync(skillName);
        if (skill == null)
        {
            // Create new skill if it doesn't exist
            skill = new Skill { Name = skillName };
            await AddAsync(skill);
            await SaveAsync();
        }

        // Check if the job-skill association already exists
        var exists = await _context.JobSkills
            .AnyAsync(js => js.JobRawId == jobRawId && js.SkillId == skill.Id);

        if (!exists)
        {
            var jobSkill = new JobSkill
            {
                JobRawId = jobRawId,
                SkillId = skill.Id
            };
            await _context.JobSkills.AddAsync(jobSkill);
        }
    }

    private static string NormalizeSkillName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        // Common cleanup when LLM returns bullets/quotes/newlines.
        var s = raw.Trim();
        s = s.Trim('"', '\'', '`');
        s = s.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

        // Collapse whitespace
        s = string.Join(' ', s.Split(' ', System.StringSplitOptions.RemoveEmptyEntries));

        // Hard cap to DB max length
        if (s.Length > SkillNameMaxLen)
            s = s[..SkillNameMaxLen];

        return s;
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
