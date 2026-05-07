namespace AiJobMarketIntelligence.Application.DTOs;

public class SalaryStatsDto
{
    public int? Min { get; init; }
    public int? Max { get; init; }
    public double? Avg { get; init; }

    public string? Currency { get; init; }
    public string? Location { get; init; }
    public string? ExperienceLevel { get; init; }
    public int? PostedWithinDays { get; init; }
}
