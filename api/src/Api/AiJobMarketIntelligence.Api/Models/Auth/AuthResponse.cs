namespace AiJobMarketIntelligence.Api.Models.Auth;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Email,
    string[] Roles
);
