using AiJobMarketIntelligence.Application.DTOs.Recommendations;

namespace AiJobMarketIntelligence.Application.Interfaces.Services.Recommendations;

public interface IJobRecommendationService
{
    Task<JobRecommendationsResultDto> GetRecommendedJobsAsync(string userId, int take = 20);
}
