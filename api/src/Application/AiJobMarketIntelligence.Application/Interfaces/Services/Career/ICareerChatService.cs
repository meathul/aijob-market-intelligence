using AiJobMarketIntelligence.Application.DTOs.Career;

namespace AiJobMarketIntelligence.Application.Interfaces.Services.Career;

public interface ICareerChatService
{
    Task<CareerChatResponseDto> AskAsync(string userId, string message);
}
