using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Domain.Entities.UserPreferences;
using AiJobMarketIntelligence.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiJobMarketIntelligence.Infrastructure.Repositories.UserPreferences;

public sealed class UserJobPreferencesRepository : IUserJobPreferencesRepository
{
    private readonly AiJobContext _db;

    public UserJobPreferencesRepository(AiJobContext db)
    {
        _db = db;
    }

    public Task<UserJobPreferences?> GetByUserIdAsync(string userId)
    {
        return _db.UserJobPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<UserJobPreferences> UpsertAsync(UserJobPreferences prefs)
    {
        var existing = await _db.UserJobPreferences.FirstOrDefaultAsync(x => x.UserId == prefs.UserId);

        if (existing is null)
        {
            prefs.UpdatedAt = DateTime.UtcNow;
            _db.UserJobPreferences.Add(prefs);
            await _db.SaveChangesAsync();
            return prefs;
        }

        existing.Location = prefs.Location;
        existing.PreferredSalaryMin = prefs.PreferredSalaryMin;
        existing.PreferredSalaryMax = prefs.PreferredSalaryMax;
        existing.PreferredJobTitle = prefs.PreferredJobTitle;
        existing.WorkMode = prefs.WorkMode;
        existing.SkillsText = prefs.SkillsText;
        existing.OnboardingCompleted = prefs.OnboardingCompleted;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return existing;
    }
}
