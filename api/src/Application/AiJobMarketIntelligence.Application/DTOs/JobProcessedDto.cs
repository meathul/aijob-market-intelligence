namespace AiJobMarketIntelligence.Application.DTOs;

/// <summary>
/// Data Transfer Object for processed job information with extracted data.
/// </summary>
public class JobProcessedDto
{
    public int Id { get; set; }
    
    public int JobRawId { get; set; }
    
    public int? SalaryMin { get; set; }
    
    public int? SalaryMax { get; set; }
    
    public string? Currency { get; set; }
    
    public string? SalaryPeriod { get; set; }
    
    public string? ExperienceLevel { get; set; }
    
    public DateTime ProcessedAt { get; set; }
}
