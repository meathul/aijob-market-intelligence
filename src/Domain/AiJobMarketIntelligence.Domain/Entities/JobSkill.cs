namespace AiJobMarketIntelligence.Domain.Entities;

/// <summary>
/// Join table representing the many-to-many relationship between Jobs and Skills.
/// </summary>
public class JobSkill
{
    public int JobRawId { get; set; }
    
    public int SkillId { get; set; }
    
    // Navigation properties
    public JobRaw JobRaw { get; set; } = null!;
    
    public Skill Skill { get; set; } = null!;
}
