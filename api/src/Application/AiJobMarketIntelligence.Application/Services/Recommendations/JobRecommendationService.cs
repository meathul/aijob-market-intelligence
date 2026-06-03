using System.Text.Json;
using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.DTOs.Recommendations;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Application.Interfaces.Services.Recommendations;
using AiJobMarketIntelligence.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace AiJobMarketIntelligence.Application.Services.Recommendations;

/// <summary>
/// Produces job recommendations by ranking jobs in the existing database using OpenAI.
/// </summary>
public sealed class JobRecommendationService : IJobRecommendationService
{
    private readonly IJobRepository _jobs;
    private readonly IUserJobPreferencesRepository _prefs;
    private readonly ChatClient _chat;
    private readonly ILogger<JobRecommendationService> _logger;

    public JobRecommendationService(
        IJobRepository jobs,
        IUserJobPreferencesRepository prefs,
        IConfiguration config,
        ILogger<JobRecommendationService> logger)
    {
        _jobs = jobs;
        _prefs = prefs;
        _logger = logger;

        // Prefer environment variable (matches how the rest of the app is configured via .env)
        // Fall back to configuration key OpenAI:ApiKey if present.
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? config["OpenAI:ApiKey"]
            ?? config["OPENAI_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is required for recommendations.");

        _chat = new ChatClient("gpt-4o-mini", apiKey);
    }

    public async Task<JobRecommendationsResultDto> GetRecommendedJobsAsync(string userId, int take = 20)
    {
        if (take <= 0) take = 20;
        if (take > 50) take = 50;

        var prefs = await _prefs.GetByUserIdAsync(userId);

        // 1) Build shortlist using fast DB-side filters first
        var shortlist = await BuildShortlistAsync(prefs);

        if (shortlist.Count == 0)
            return new JobRecommendationsResultDto();

        // 2) Ask LLM to rank shortlist
        var ranked = await RankWithAiAsync(prefs, shortlist, take);

        // 3) Map back to DTO
        var byId = shortlist.ToDictionary(j => j.Id);
        var result = new JobRecommendationsResultDto();

        foreach (var r in ranked)
        {
            if (!byId.TryGetValue(r.JobId, out var job))
                continue;

            result.Jobs.Add(new JobRecommendationDto
            {
                Job = MapToDto(job),
                Score = r.Score,
                Reason = r.Reason
            });
        }

        // fallback if AI returned nothing usable
        if (result.Jobs.Count == 0)
        {
            result.Jobs = shortlist
                .OrderByDescending(j => j.PostedDate)
                .Take(take)
                .Select(j => new JobRecommendationDto { Job = MapToDto(j), Score = 0.5, Reason = "Recent match" })
                .ToList();
        }

        return result;
    }

    private async Task<List<JobRaw>> BuildShortlistAsync(Domain.Entities.UserPreferences.UserJobPreferences? prefs)
    {
        // Pull a bigger set, then let the AI do the final ranking.
        // We keep it limited to reduce tokens.
        const int initial = 60;

        // Prefer processed jobs (have richer normalized data + skills populated)
        var rows = await _jobs.GetProcessedAsync(pageNumber: 1, pageSize: initial);

        // Basic filtering
        var q = (prefs?.PreferredJobTitle ?? string.Empty).Trim();
        var loc = (prefs?.Location ?? string.Empty).Trim();
        var workMode = (prefs?.WorkMode ?? string.Empty).Trim();

        IEnumerable<JobRaw> filtered = rows;

        if (!string.IsNullOrWhiteSpace(loc))
        {
            filtered = filtered.Where(j => (j.Location ?? string.Empty).Contains(loc, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            filtered = filtered.Where(j => (j.Title ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(workMode) && !string.Equals(workMode, "Any", StringComparison.OrdinalIgnoreCase))
        {
            // Light heuristic: "Remote" means Location exactly Remote OR contains Remote
            if (string.Equals(workMode, "Remote", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(j => (j.Location ?? string.Empty).Contains("remote", StringComparison.OrdinalIgnoreCase));
        }

        return filtered.Take(initial).ToList();
    }

    private async Task<List<AiRankedItem>> RankWithAiAsync(
        Domain.Entities.UserPreferences.UserJobPreferences? prefs,
        List<JobRaw> shortlist,
        int take)
    {
        // Keep job payload compact
        var jobsPayload = shortlist.Select(j => new
        {
            id = j.Id,
            title = j.Title,
            company = j.Company,
            location = j.Location,
            salary = j.SalaryRaw,
            skills = j.JobSkills?.Select(x => x.Skill.Name).Distinct().Take(20).ToArray() ?? Array.Empty<string>()
        }).ToList();

        var system =
            "You are a job matching engine. Rank jobs for a user based on their preferences.\n\n" +
            "Rules:\n" +
            "- Return ONLY valid JSON.\n" +
            "- Output must be an array of objects: [{\"jobId\": number, \"score\": number, \"reason\": string}].\n" +
            "- score is 0..1 (1 is best).\n" +
            "- Keep reason under 140 characters.\n" +
            "- Only include jobIds that exist in the input.\n";

        var user = JsonSerializer.Serialize(new
        {
            preferences = new
            {
                location = prefs?.Location,
                preferredJobTitle = prefs?.PreferredJobTitle,
                preferredSalaryMin = prefs?.PreferredSalaryMin,
                preferredSalaryMax = prefs?.PreferredSalaryMax,
                workMode = prefs?.WorkMode,
                skillsText = prefs?.SkillsText
            },
            jobs = jobsPayload,
            take
        });

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(system),
            new UserChatMessage(user)
        };

        try
        {
            var resp = await _chat.CompleteChatAsync(messages);
            var text = resp.Value.Content[0].Text ?? "[]";

            // basic cleanup if model wraps in markdown code fences
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                var firstNl = text.IndexOf('\n');
                if (firstNl > 0) text = text[(firstNl + 1)..];
                text = text.Replace("```", "").Trim();
            }

            var items = JsonSerializer.Deserialize<List<AiRankedItem>>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<AiRankedItem>();

            return items
                .Where(i => i.JobId > 0)
                .OrderByDescending(i => i.Score)
                .Take(take)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI ranking failed; falling back to recency.");
            return new();
        }
    }

    private static JobRawDto MapToDto(JobRaw job)
    {
        return new JobRawDto
        {
            Id = job.Id,
            Title = job.Title,
            Company = job.Company,
            Location = job.Location,
            Description = job.Description,
            SalaryRaw = job.SalaryRaw,
            Source = job.Source,
            Url = job.Url,
            PostedDate = job.PostedDate,
            CreatedAt = job.CreatedAt,
            IsProcessed = job.IsProcessed,
            Skills = job.JobSkills?.Select(js => new JobSkillDto
            {
                SkillId = js.SkillId,
                SkillName = js.Skill.Name
            }).ToList() ?? new()
        };
    }

    private sealed class AiRankedItem
    {
        public int JobId { get; set; }
        public double Score { get; set; }
        public string? Reason { get; set; }
    }
}
