namespace AiJobMarketIntelligence.Domain.Entities;

/// <summary>
/// Represents a skill that can be associated with jobs.
/// </summary>
public class Skill
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    
    // Navigation property
    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}
