namespace AiJobMarketIntelligence.Application.DTOs;

public class SystemStatsDto
{
    public int TotalJobs { get; init; }
    public int ProcessedJobs { get; init; }
    public int UnprocessedJobs { get; init; }

    public int TotalSkills { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
