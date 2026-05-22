namespace AiJobMarketIntelligence.Api.Models.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password
);
