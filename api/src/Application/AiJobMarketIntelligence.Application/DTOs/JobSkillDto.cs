namespace AiJobMarketIntelligence.Application.DTOs;

/// <summary>
/// Data Transfer Object for job skill information.
/// </summary>
public class JobSkillDto
{
    public int SkillId { get; set; }
    
    public string SkillName { get; set; } = string.Empty;
}
