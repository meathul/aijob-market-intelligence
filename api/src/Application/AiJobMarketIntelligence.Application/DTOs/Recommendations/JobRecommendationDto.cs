using AiJobMarketIntelligence.Application.DTOs;

namespace AiJobMarketIntelligence.Application.DTOs.Recommendations;

public sealed class JobRecommendationDto
{
    public JobRawDto Job { get; set; } = new();

    public double Score { get; set; }

    public string? Reason { get; set; }
}
