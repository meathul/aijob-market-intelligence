using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AiJobMarketIntelligence.Infrastructure.Data;

/// <summary>
/// Application user entity for Identity.
/// Kept in Infrastructure so Domain remains free of ASP.NET Core dependencies.
/// </summary>
public class ApplicationUser : IdentityUser
{
}

/// <summary>
/// Identity database context. Uses the same MySQL database as the app.
/// Kept separate from AiJobContext to avoid coupling Identity tables with the job market model.
/// </summary>
public class AuthDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }
}
