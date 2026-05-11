using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Application.Interfaces.Repositories;

public interface ISkillRepository
{
    Task<Skill?> GetByIdAsync(int id);
    Task<Skill?> GetByNameAsync(string name);
    Task<List<Skill>> GetAllAsync();

    Task AddAsync(Skill skill);
    Task AddRangeAsync(IEnumerable<Skill> skills);

    Task<bool> ExistsByNameAsync(string name);

    // Job skill associations
    Task<List<string>> GetSkillsByJobRawIdAsync(int jobRawId);
    Task AddJobSkillAsync(int jobRawId, string skillName);
    Task<List<JobSkill>> GetJobSkillsByJobRawIdAsync(int jobRawId);

    Task SaveAsync();
}
