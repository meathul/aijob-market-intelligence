namespace AiJobMarketIntelligence.Domain.Entities;

/// <summary>
/// Represents processed job data with extracted and normalized information.
/// This entity is created after AI processing of raw job data.
/// </summary>
public class JobProcessed
{
    public int Id { get; set; }
    
    public int JobRawId { get; set; }
    
    public int? SalaryMin { get; set; }
    
    public int? SalaryMax { get; set; }
    
    public string? Currency { get; set; }
    
    public SalaryPeriod SalaryPeriod { get; set; } = SalaryPeriod.Unknown;
    
    public string? ExperienceLevel { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public JobRaw JobRaw { get; set; } = null!;
}
