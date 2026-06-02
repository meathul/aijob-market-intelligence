using AiJobMarketIntelligence.Domain.Entities.UserPreferences;

namespace AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;

public interface IUserJobPreferencesRepository
{
    Task<UserJobPreferences?> GetByUserIdAsync(string userId);

    Task<UserJobPreferences> UpsertAsync(UserJobPreferences prefs);
}
