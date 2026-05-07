namespace AiJobMarketIntelligence.Application.DTOs;

/// <summary>
/// Data Transfer Object for job search results with pagination.
/// </summary>
public class JobSearchResultDto
{
    public List<JobRawDto> Jobs { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
