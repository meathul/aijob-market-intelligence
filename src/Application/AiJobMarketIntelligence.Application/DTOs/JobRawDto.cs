namespace AiJobMarketIntelligence.Application.DTOs;

/// <summary>
/// Data Transfer Object for raw job information returned from API.
/// </summary>
public class JobRawDto
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Company { get; set; } = string.Empty;
    
    public string Location { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string? SalaryRaw { get; set; }
    
    public string Source { get; set; } = string.Empty;
    
    public string Url { get; set; } = string.Empty;
    
    public DateTime PostedDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsProcessed { get; set; }
    
    public List<JobSkillDto> Skills { get; set; } = new();
}
