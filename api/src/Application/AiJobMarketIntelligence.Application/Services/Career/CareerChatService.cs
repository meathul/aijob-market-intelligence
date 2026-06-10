using AiJobMarketIntelligence.Application.DTOs.Career;
using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Application.Interfaces.Services.Career;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace AiJobMarketIntelligence.Application.Services.Career;

public sealed class CareerChatService : ICareerChatService
{
    private const int MaxMessageLength = 2000;

    private readonly IUserJobPreferencesRepository _prefs;
    private readonly ChatClient _chat;
    private readonly ILogger<CareerChatService> _logger;

    public CareerChatService(
        IUserJobPreferencesRepository prefs,
        IConfiguration config,
        ILogger<CareerChatService> logger)
    {
        _prefs = prefs;
        _logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? config["OpenAI:ApiKey"]
            ?? config["OPENAI_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is required for career chat.");

        _chat = new ChatClient("gpt-4o-mini", apiKey);
    }

    public async Task<CareerChatResponseDto> AskAsync(string userId, string message)
    {
        var trimmed = (message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return new CareerChatResponseDto { Answer = "Ask me a career question and I can help." };

        if (trimmed.Length > MaxMessageLength)
            trimmed = trimmed[..MaxMessageLength];

        var prefs = await _prefs.GetByUserIdAsync(userId);
        var profileContext =
            $"Preferred role: {prefs?.PreferredJobTitle ?? "Any"}\n" +
            $"Location: {prefs?.Location ?? "Any"}\n" +
            $"Work mode: {prefs?.WorkMode ?? "Any"}\n" +
            $"Salary min: {prefs?.PreferredSalaryMin?.ToString() ?? "Any"}\n" +
            $"Salary max: {prefs?.PreferredSalaryMax?.ToString() ?? "Any"}\n" +
            $"Skills: {prefs?.SkillsText ?? "Not provided"}";

        var system =
            "You are Career Bot for an AI job market intelligence app.\n" +
            "Answer only career, job-search, resume, interview, salary negotiation, skill planning, and job-market questions.\n" +
            "Use the user's saved job preferences as context when helpful.\n" +
            "Keep answers practical, specific, and concise. Use bullets when it improves clarity.\n" +
            "If the user asks for something outside career guidance, politely redirect them to career topics.\n" +
            "Do not claim certainty about live job availability unless the user provides it.\n" +
            "Do not provide legal, medical, tax, or financial advice; offer general career guidance instead.";

        var user =
            $"User profile:\n{profileContext}\n\n" +
            $"Question:\n{trimmed}";

        try
        {
            var response = await _chat.CompleteChatAsync(new List<ChatMessage>
            {
                new SystemChatMessage(system),
                new UserChatMessage(user)
            });

            var answer = response.Value.Content[0].Text?.Trim();
            return new CareerChatResponseDto
            {
                Answer = string.IsNullOrWhiteSpace(answer)
                    ? "I could not generate an answer just now. Try asking again with a little more detail."
                    : answer
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Career chat failed.");
            return new CareerChatResponseDto
            {
                Answer = "I could not reach the career chat service right now. Please try again shortly."
            };
        }
    }
}
