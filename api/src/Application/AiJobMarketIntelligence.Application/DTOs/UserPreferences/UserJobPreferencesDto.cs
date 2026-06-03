namespace AiJobMarketIntelligence.Application.DTOs.UserPreferences;

public sealed class UserJobPreferencesDto
{
    public string? Location { get; set; }

    public int? PreferredSalaryMin { get; set; }

    public int? PreferredSalaryMax { get; set; }

    public string? PreferredJobTitle { get; set; }

    public string? WorkMode { get; set; } // Remote | Hybrid | Onsite | Any

    public string? SkillsText { get; set; }

    public bool OnboardingCompleted { get; set; }
}
