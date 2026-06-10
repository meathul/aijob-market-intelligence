using System.Text.Json;
using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.DTOs.Recommendations;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Application.Interfaces.Services.Recommendations;
using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Domain.Entities.UserPreferences;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace AiJobMarketIntelligence.Application.Services.Recommendations;

/// <summary>
/// Produces job recommendations by ranking jobs in the existing database using OpenAI.
/// </summary>
public sealed class JobRecommendationService : IJobRecommendationService
{
    private const int CandidatePoolSize = 120;
    private const int ShortlistSize = 80;

    private readonly IJobRepository _jobs;
    private readonly IUserJobPreferencesRepository _prefs;
    private readonly ChatClient? _chat;
    private readonly bool _aiEnabled;
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

        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? config["GROQ_API_KEY"]
            ?? config["Groq:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? config["OpenAI:ApiKey"]
            ?? config["OPENAI_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _aiEnabled = false;
            _logger.LogWarning("GROQ_API_KEY/OPENAI_API_KEY is not set; recommendations will use profile-based scoring only.");
        }
        else
        {
            _aiEnabled = true;
            var options = new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") };
            _chat = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options).GetChatClient("llama-3.1-8b-instant");
        }
    }

    public async Task<JobRecommendationsResultDto> GetRecommendedJobsAsync(string userId, int take = 20)
    {
        if (take <= 0) take = 20;
        if (take > 50) take = 50;

        var prefs = await _prefs.GetByUserIdAsync(userId);
        var hasPrefs = HasMeaningfulPreferences(prefs);

        _logger.LogInformation(
            "Building recommendations for user {UserId}. Preferences found: {HasPrefs}",
            userId,
            hasPrefs);

        var shortlist = await BuildShortlistAsync(prefs);

        _logger.LogInformation("Recommendation shortlist size: {Count}", shortlist.Count);

        if (shortlist.Count == 0)
            return new JobRecommendationsResultDto();

        List<AiRankedItem> ranked;

        if (_aiEnabled && _chat is not null)
            ranked = await RankWithAiAsync(prefs, shortlist, take);
        else
            ranked = new();

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

        if (result.Jobs.Count == 0)
        {
            result.Jobs = RankByProfileHeuristic(prefs, shortlist, take)
                .Select(x => new JobRecommendationDto
                {
                    Job = MapToDto(x.Job),
                    Score = x.Score,
                    Reason = x.Reason
                })
                .ToList();
        }

        return result;
    }

    private async Task<List<JobRaw>> BuildShortlistAsync(UserJobPreferences? prefs)
    {
        var rows = await _jobs.GetProcessedAsync(pageNumber: 1, pageSize: CandidatePoolSize);

        if (rows.Count == 0)
            return rows;

        var preferredSkills = ParseSkillTokens(prefs?.SkillsText);
        var titleQuery = (prefs?.PreferredJobTitle ?? string.Empty).Trim();
        var locationQuery = (prefs?.Location ?? string.Empty).Trim();
        var workMode = (prefs?.WorkMode ?? string.Empty).Trim();

        var scored = rows
            .Select(j => (Job: j, Score: ScoreJob(j, prefs, preferredSkills, titleQuery, locationQuery, workMode)))
            .ToList();

        // Prefer jobs that match profile; still include weaker matches if the pool is small.
        var strict = scored
            .Where(x => x.Score >= 0.35)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Job.PostedDate)
            .Select(x => x.Job)
            .Take(ShortlistSize)
            .ToList();

        if (strict.Count >= Math.Min(10, ShortlistSize))
            return strict;

        var relaxed = scored
            .Where(x => PassesRelaxedFilters(x.Job, titleQuery, locationQuery, workMode))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Job.PostedDate)
            .Select(x => x.Job)
            .Take(ShortlistSize)
            .ToList();

        if (relaxed.Count > 0)
            return relaxed;

        return scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Job.PostedDate)
            .Select(x => x.Job)
            .Take(ShortlistSize)
            .ToList();
    }

    private static bool PassesRelaxedFilters(
        JobRaw job,
        string titleQuery,
        string locationQuery,
        string workMode)
    {
        if (!string.IsNullOrWhiteSpace(titleQuery) &&
            !TitleMatches(job.Title, titleQuery))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(locationQuery) &&
            !LocationMatches(job.Location, locationQuery))
        {
            return false;
        }

        return MatchesWorkMode(job.Location, workMode);
    }

    private static double ScoreJob(
        JobRaw job,
        UserJobPreferences? prefs,
        IReadOnlyList<string> preferredSkills,
        string titleQuery,
        string locationQuery,
        string workMode)
    {
        var score = 0.0;

        if (!string.IsNullOrWhiteSpace(titleQuery))
        {
            score += TitleMatches(job.Title, titleQuery) ? 0.35 : 0;
            if (!TitleMatches(job.Title, titleQuery) &&
                TokenOverlap(job.Title, titleQuery) > 0)
            {
                score += 0.15;
            }
        }

        if (!string.IsNullOrWhiteSpace(locationQuery))
        {
            score += LocationMatches(job.Location, locationQuery) ? 0.25 : 0;
        }

        if (!string.IsNullOrWhiteSpace(workMode) &&
            !string.Equals(workMode, "Any", StringComparison.OrdinalIgnoreCase))
        {
            score += MatchesWorkMode(job.Location, workMode) ? 0.15 : -0.1;
        }

        if (preferredSkills.Count > 0)
        {
            var jobSkills = job.JobSkills?
                .Select(x => x.Skill.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList() ?? new List<string>();

            var overlap = preferredSkills.Count(ps =>
                jobSkills.Any(js => js.Contains(ps, StringComparison.OrdinalIgnoreCase) ||
                                    ps.Contains(js, StringComparison.OrdinalIgnoreCase)));

            score += Math.Min(0.35, overlap * 0.08);
        }

        if (prefs?.PreferredSalaryMin is not null || prefs?.PreferredSalaryMax is not null)
        {
            // Salary strings are unstructured; light boost when a range appears present.
            if (!string.IsNullOrWhiteSpace(job.SalaryRaw))
                score += 0.05;
        }

        if (job.PostedDate != default)
        {
            var ageDays = (DateTime.UtcNow - job.PostedDate).TotalDays;
            if (ageDays <= 14) score += 0.05;
            else if (ageDays <= 30) score += 0.02;
        }

        return Math.Clamp(score, 0, 1);
    }

    private static bool TitleMatches(string? title, string query)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(query))
            return false;

        return title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               query.Contains(title, StringComparison.OrdinalIgnoreCase);
    }

    private static bool LocationMatches(string? location, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var loc = location ?? string.Empty;
        if (loc.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;

        var queryTokens = Tokenize(query);
        var locTokens = Tokenize(loc);
        return queryTokens.Any(qt => locTokens.Any(lt =>
            lt.Contains(qt, StringComparison.OrdinalIgnoreCase) ||
            qt.Contains(lt, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool MatchesWorkMode(string? location, string workMode)
    {
        if (string.IsNullOrWhiteSpace(workMode) ||
            string.Equals(workMode, "Any", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var loc = (location ?? string.Empty).ToLowerInvariant();

        if (string.Equals(workMode, "Remote", StringComparison.OrdinalIgnoreCase))
            return loc.Contains("remote") || loc.Contains("work from home") || loc.Contains("wfh");

        if (string.Equals(workMode, "Hybrid", StringComparison.OrdinalIgnoreCase))
            return loc.Contains("hybrid");

        if (string.Equals(workMode, "Onsite", StringComparison.OrdinalIgnoreCase))
            return !loc.Contains("remote") && !loc.Contains("work from home") && !string.IsNullOrWhiteSpace(loc);

        return true;
    }

    private static int TokenOverlap(string? a, string? b)
    {
        var ta = Tokenize(a ?? string.Empty);
        var tb = Tokenize(b ?? string.Empty);
        return ta.Count(t => tb.Any(u => u.Equals(t, StringComparison.OrdinalIgnoreCase)));
    }

    private static List<string> Tokenize(string value) =>
        value.Split(new[] { ' ', ',', '/', '-', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .ToList();

    private static List<string> ParseSkillTokens(string? skillsText)
    {
        if (string.IsNullOrWhiteSpace(skillsText))
            return new List<string>();

        return skillsText
            .Split(new[] { ',', ';', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool HasMeaningfulPreferences(UserJobPreferences? prefs)
    {
        if (prefs is null) return false;

        if (!prefs.OnboardingCompleted) return false;

        return !string.IsNullOrWhiteSpace(prefs.Location) ||
               !string.IsNullOrWhiteSpace(prefs.PreferredJobTitle) ||
               !string.IsNullOrWhiteSpace(prefs.SkillsText) ||
               prefs.PreferredSalaryMin is > 0 ||
               prefs.PreferredSalaryMax is > 0 ||
               (!string.IsNullOrWhiteSpace(prefs.WorkMode) &&
                !string.Equals(prefs.WorkMode, "Any", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<AiRankedItem>> RankWithAiAsync(
        UserJobPreferences? prefs,
        List<JobRaw> shortlist,
        int take)
    {
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
            "- Prioritize preferred job title, skills, location, work mode, and salary range.\n" +
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
            var resp = await _chat!.CompleteChatAsync(messages);
            var text = resp.Value.Content[0].Text ?? "[]";

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
            _logger.LogWarning(ex, "AI ranking failed; falling back to profile-based scoring.");
            return new();
        }
    }

    private static List<(JobRaw Job, double Score, string Reason)> RankByProfileHeuristic(
        UserJobPreferences? prefs,
        List<JobRaw> shortlist,
        int take)
    {
        var preferredSkills = ParseSkillTokens(prefs?.SkillsText);
        var titleQuery = (prefs?.PreferredJobTitle ?? string.Empty).Trim();
        var locationQuery = (prefs?.Location ?? string.Empty).Trim();
        var workMode = (prefs?.WorkMode ?? string.Empty).Trim();

        return shortlist
            .Select(j =>
            {
                var score = ScoreJob(j, prefs, preferredSkills, titleQuery, locationQuery, workMode);
                var reason = BuildHeuristicReason(j, prefs, preferredSkills, score);
                return (Job: j, Score: score, Reason: reason);
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Job.PostedDate)
            .Take(take)
            .ToList();
    }

    private static string BuildHeuristicReason(
        JobRaw job,
        UserJobPreferences? prefs,
        IReadOnlyList<string> preferredSkills,
        double score)
    {
        if (prefs is null || !HasMeaningfulPreferences(prefs))
            return "Recent processed job";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(prefs.PreferredJobTitle) &&
            TitleMatches(job.Title, prefs.PreferredJobTitle))
        {
            parts.Add("title match");
        }

        if (!string.IsNullOrWhiteSpace(prefs.Location) &&
            LocationMatches(job.Location, prefs.Location))
        {
            parts.Add("location match");
        }

        if (preferredSkills.Count > 0)
        {
            var jobSkills = job.JobSkills?.Select(x => x.Skill.Name).ToList() ?? new List<string>();
            var hits = preferredSkills.Count(ps =>
                jobSkills.Any(js => js.Contains(ps, StringComparison.OrdinalIgnoreCase)));
            if (hits > 0)
                parts.Add($"{hits} skill match(es)");
        }

        if (parts.Count == 0)
            return score >= 0.5 ? "Good overall fit" : "Partial profile fit";

        return string.Join(", ", parts);
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
