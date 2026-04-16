namespace AiJobMarketIntelligence.Domain.Entities;

/// <summary>
/// Represents raw job data as ingested from external sources.
/// This is the initial storage point before any processing.
/// </summary>
public class JobRaw
{
    public int Id { get; set; }
    
    public required string Title { get; set; }
    
    public required string Company { get; set; }
    
    public required string Location { get; set; }
    
    public required string Description { get; set; }
    
    public string? SalaryRaw { get; set; }
    
    public required string Source { get; set; }
    
    public required string Url { get; set; }
    
    public DateTime PostedDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsProcessed { get; set; }
    
    // Navigation property
    public JobProcessed? JobProcessed { get; set; }
    
    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}
