using AiJobMarketIntelligence.Application.DTOs.Career;
using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Application.Interfaces.Services.Career;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace AiJobMarketIntelligence.Application.Services.Career;

public sealed class CareerChatService : ICareerChatService
{
    private const int MaxMessageLength = 2000;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, List<ChatMessage>> _chatHistory = new();

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

        var apiKey = FirstNonBlank(
            Environment.GetEnvironmentVariable("GROQ_API_KEY"),
            config["GROQ_API_KEY"],
            config["Groq:ApiKey"],
            config["OpenAI:ApiKey"],
            Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            config["OPENAI_API_KEY"]);

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is required for career chat.");

        var options = new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") };
        _chat = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options).GetChatClient("llama-3.1-8b-instant");
    }

    public async Task<CareerChatResponseDto> AskAsync(string userId, string message, List<ChatMessageDto>? historyDto = null)
    {
        var trimmed = (message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return new CareerChatResponseDto { Answer = "Ask me a career question and I can help." };

        if (trimmed.Length > MaxMessageLength)
            trimmed = trimmed[..MaxMessageLength];

        // Support manual history resets
        if (trimmed.Equals("reset", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("clear", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("clear chat", StringComparison.OrdinalIgnoreCase))
        {
            _chatHistory.TryRemove(userId, out _);
            return new CareerChatResponseDto { Answer = "I've reset our conversation history. How can I assist you today?" };
        }

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
            "Answer career, job-search, resume, interview, salary negotiation, skill planning, and job-market questions, as well as basic greetings or questions about the conversation context (such as the user's name if mentioned in the chat history).\n" +
            "Keep answers practical, specific, and concise. Use bullets when it improves clarity.\n" +
            "If the user asks for something completely unrelated to career guidance (like general knowledge, weather, or sports), politely redirect them to career topics.\n" +
            "Do not claim certainty about live job availability unless the user provides it.\n" +
            "Do not provide legal, medical, tax, or financial advice; offer general career guidance instead.\n\n" +
            $"User profile context:\n{profileContext}";

        List<ChatMessage> history;
        bool isUsingStaticHistory = false;

        if (historyDto != null && historyDto.Count > 0)
        {
            history = new List<ChatMessage> { new SystemChatMessage(system) };
            foreach (var msg in historyDto)
            {
                if (string.Equals(msg.Role, "user", StringComparison.OrdinalIgnoreCase))
                {
                    history.Add(new UserChatMessage(msg.Content));
                }
                else if (string.Equals(msg.Role, "assistant", StringComparison.OrdinalIgnoreCase) || 
                         string.Equals(msg.Role, "bot", StringComparison.OrdinalIgnoreCase))
                {
                    history.Add(new AssistantChatMessage(msg.Content));
                }
            }
        }
        else
        {
            // Retrieve or initialize user chat history from in-memory dictionary
            history = _chatHistory.GetOrAdd(userId, _ => new List<ChatMessage>
            {
                new SystemChatMessage(system)
            });

            // Ensure the system instruction contains the latest profile context in case preferences updated
            if (history.Count > 0 && history[0] is SystemChatMessage)
            {
                history[0] = new SystemChatMessage(system);
            }
            isUsingStaticHistory = true;
        }

        // Add the new user message
        history.Add(new UserChatMessage(trimmed));

        // Limit context size to system prompt + last 12 messages (6 turns) to avoid context limit issues
        if (history.Count > 13)
        {
            if (isUsingStaticHistory)
            {
                history.RemoveRange(1, 2); // remove oldest message pair in place
            }
            else
            {
                while (history.Count > 13)
                {
                    history.RemoveAt(1); // remove oldest message after system message
                }
            }
        }

        try
        {
            var response = await _chat.CompleteChatAsync(history);

            var answer = string.Join(
                    "",
                    response.Value.Content
                        .Select(part => part.Text)
                        .Where(text => !string.IsNullOrWhiteSpace(text)))
                .Trim();

            if (string.IsNullOrWhiteSpace(answer))
            {
                answer = "I could not generate an answer just now. Try asking again with a little more detail.";
            }

            // Append assistant response to history
            history.Add(new AssistantChatMessage(answer));

            return new CareerChatResponseDto
            {
                Answer = answer
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Career chat failed. Falling back to simulated response.");
            
            var fallbackAnswer = GenerateFallbackResponse(prefs, trimmed);
            
            // Append fallback response to history to preserve dialog flow
            history.Add(new AssistantChatMessage(fallbackAnswer));

            return new CareerChatResponseDto
            {
                Answer = fallbackAnswer
            };
        }
    }

    private static string GenerateFallbackResponse(
        AiJobMarketIntelligence.Domain.Entities.UserPreferences.UserJobPreferences? prefs, 
        string message)
    {
        var role = prefs?.PreferredJobTitle?.Trim() ?? "Software Engineer";
        var skills = string.IsNullOrWhiteSpace(prefs?.SkillsText) ? "technical skills" : prefs.SkillsText;
        var location = prefs?.Location?.Trim() ?? "Remote";
        var mode = prefs?.WorkMode?.Trim() ?? "Remote";
        var minSalary = prefs?.PreferredSalaryMin?.ToString() ?? "market rates";
        var maxSalary = prefs?.PreferredSalaryMax?.ToString() ?? "market rates";

        var query = (message ?? string.Empty).ToLowerInvariant();

        string responseBody;

        if (query.Contains("resume") || query.Contains("cv") || query.Contains("portfolio"))
        {
            responseBody = 
                $"Here are key tips for tailoring your resume for **{role}** roles:\n\n" +
                $"• **Highlight Core Skills**: Feature **{skills}** prominently in your skills section.\n" +
                $"• **Add Projects**: Describe projects where you implemented these skills, emphasizing business impact.\n" +
                $"• **Target Your Work Preference**: Clearly state your availability for **{mode}** work.\n" +
                $"• **Tailor the Profile**: Write a strong summary aligning yourself directly with the **{role}** job description.";
        }
        else if (query.Contains("interview") || query.Contains("prepare") || query.Contains("question"))
        {
            responseBody = 
                $"To prepare for a **{role}** interview, I recommend focusing on these areas:\n\n" +
                $"• **Technical Questions**: Expect questions testing your depth in **{skills}**.\n" +
                $"• **Behavioral Scenarios**: Use the STAR method to describe situations where you resolved conflicts or solved difficult coding problems.\n" +
                $"• **Work Mode Readiness**: Be ready to talk about how you operate effectively in a **{mode}** setting.\n" +
                $"• **Ask Questions**: Prepare questions for the interviewers about their tech stack, codebase size, and team structure.";
        }
        else if (query.Contains("salary") || query.Contains("negotiate") || query.Contains("pay") || query.Contains("earn"))
        {
            responseBody = 
                $"Regarding salary negotiations for a **{role}** role:\n\n" +
                $"• **Know Your Target**: Your profile lists a preferred range of **{minSalary} - {maxSalary}**. Keep this target clear.\n" +
                $"• **Research Location Premiums**: Salaries in **{location}** vary based on local cost of living and remote status.\n" +
                $"• **Negotiate the Full Package**: Look at health benefits, equity, and remote work flexibility in addition to base salary.\n" +
                $"• **Anchor High**: Let the employer make the first offer if possible, then negotiate upward using your technical expertise in **{skills}**.";
        }
        else if (query.Contains("skill") || query.Contains("learn") || query.Contains("study") || query.Contains("course"))
        {
            responseBody = 
                $"To advance your career as a **{role}**, consider the following skill development plan:\n\n" +
                $"• **Strengthen Core Skills**: Double down on mastering **{skills}**.\n" +
                $"• **Add Database/Cloud Competence**: Standard industry benchmarks recommend adding SQL/NoSQL or AWS/GCP certification.\n" +
                $"• **Build Public Repositories**: Showcase your learning through active projects on GitHub.\n" +
                $"• **Read Technical Content**: Stay updated with engineering blogs and modern system architecture articles.";
        }
        else
        {
            responseBody = 
                $"Hello! I am your Career Assistant. I analyzed your profile targeting **{role}** roles (Location: **{location}**, Work mode: **{mode}**).\n\n" +
                $"Based on your profile skills (**{skills}**), here is how to approach your career goals:\n" +
                $"• Focus your job search on platforms listing **{role}** openings.\n" +
                $"• Tailor your applications to highlight your technical background.\n" +
                $"• Keep your profile preferences updated to receive optimal recommendations.\n\n" +
                $"Feel free to ask me questions specifically about **resumes**, **interview prep**, **salary negotiations**, or **skill development**!";
        }

        return responseBody + "\n\n*(Note: Running in offline fallback mode due to Groq API quota or connection issues)*";
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
}
