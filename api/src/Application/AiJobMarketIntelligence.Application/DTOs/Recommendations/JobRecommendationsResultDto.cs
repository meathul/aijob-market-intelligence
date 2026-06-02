namespace AiJobMarketIntelligence.Application.DTOs.Recommendations;

public sealed class JobRecommendationsResultDto
{
    public List<JobRecommendationDto> Jobs { get; set; } = new();
}
