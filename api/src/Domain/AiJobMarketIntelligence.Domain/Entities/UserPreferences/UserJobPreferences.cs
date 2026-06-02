namespace AiJobMarketIntelligence.Domain.Entities.UserPreferences;

/// <summary>
/// Stores job preference inputs collected during onboarding, per authenticated user.
/// </summary>
public sealed class UserJobPreferences
{
    public int Id { get; set; }

    /// <summary>
    /// Identity user id (string). AuthDbContext uses string keys.
    /// </summary>
    public required string UserId { get; set; }

    public string? Location { get; set; }

    public int? PreferredSalaryMin { get; set; }

    public int? PreferredSalaryMax { get; set; }

    public string? PreferredJobTitle { get; set; }

    public string? WorkMode { get; set; } // Remote | Hybrid | Onsite | Any

    public string? SkillsText { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
