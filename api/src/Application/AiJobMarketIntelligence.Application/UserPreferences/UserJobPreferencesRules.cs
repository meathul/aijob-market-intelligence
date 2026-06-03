using AiJobMarketIntelligence.Application.DTOs.UserPreferences;

namespace AiJobMarketIntelligence.Application.UserPreferences;

public static class UserJobPreferencesRules
{
    public static bool HasMeaningfulPreferences(UserJobPreferencesDto? dto)
    {
        if (dto is null) return false;

        var hasSalary =
            (dto.PreferredSalaryMin is > 0) ||
            (dto.PreferredSalaryMax is > 0);

        return !string.IsNullOrWhiteSpace(dto.Location) ||
               !string.IsNullOrWhiteSpace(dto.PreferredJobTitle) ||
               !string.IsNullOrWhiteSpace(dto.SkillsText) ||
               hasSalary ||
               (!string.IsNullOrWhiteSpace(dto.WorkMode) &&
                !string.Equals(dto.WorkMode, "Any", StringComparison.OrdinalIgnoreCase));
    }
}
