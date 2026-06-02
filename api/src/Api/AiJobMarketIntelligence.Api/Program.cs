using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using AiJobMarketIntelligence.Infrastructure.Data;
using AiJobMarketIntelligence.Infrastructure.Repositories;
using AiJobMarketIntelligence.Application.Services;
using AiJobMarketIntelligence.Application.Services.Providers;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Interfaces.Services;
using AiJobMarketIntelligence.Application.Services.Salary;
using AiJobMarketIntelligence.Application.Services.Skills;
using AiJobMarketIntelligence.Application.Services.Processing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AiJobMarketIntelligence.Api.Services;
using AiJobMarketIntelligence.Infrastructure.Repositories.UserPreferences;
using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Application.Interfaces.Services.Recommendations;
using AiJobMarketIntelligence.Application.Services.Recommendations;

// Load .env before building configuration (so builder.Configuration sees these as env vars)
DotEnvBootstrap.LoadFromRepoRoot(Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Back-compat: allow JWT_KEY env var to satisfy Jwt:Key setting
if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
{
    var jwtFromEnv = Environment.GetEnvironmentVariable("JWT_KEY");
    if (!string.IsNullOrWhiteSpace(jwtFromEnv))
        builder.Configuration["Jwt:Key"] = jwtFromEnv;
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// Configure Entity Framework Core with MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AiJobContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.MigrationsAssembly("AiJobMarketIntelligence.Infrastructure")));

// Identity DbContext (same MySQL database)
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.MigrationsAssembly("AiJobMarketIntelligence.Infrastructure")));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddSignInManager();

// JWT auth
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("JWT signing key is required. Set Jwt:Key in configuration or JWT_KEY environment variable.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "AiJobMarketIntelligence";

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "AiJobMarketIntelligence";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Register repositories (Infrastructure implementations for Application interfaces)
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IJobProcessedRepository, JobProcessedRepository>();
builder.Services.AddScoped<IUserJobPreferencesRepository, UserJobPreferencesRepository>();

// Salary parsing + skill extraction + processing pipeline (processing runs in Worker; API reuses services for any ad-hoc processing needs)
builder.Services.AddSingleton<ISalaryParserService, SalaryParserService>();

// Register OpenAI skill extraction service
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OpenAI API key is required. Set it via configuration or OPENAI_API_KEY environment variable.");

builder.Services.AddSingleton<ISkillExtractionService>(sp =>
    new OpenAiSkillExtractionService(openAiApiKey, sp.GetRequiredService<ILogger<OpenAiSkillExtractionService>>()));

builder.Services.AddScoped<IJobProcessingService, JobProcessingService>();

// Register job query service
builder.Services.AddScoped<IJobQueryService, JobQueryService>();

// Recommendations (AI ranking over DB jobs)
builder.Services.AddScoped<IJobRecommendationService, JobRecommendationService>();

// Register Adzuna provider (real job data from free Adzuna API)
builder.Services.AddScoped<AdzunaJobProvider>();

// NEW: Live Adzuna provider (requires ADZUNA_APP_ID/ADZUNA_APP_KEY)
builder.Services.AddHttpClient<AdzunaLiveJobProvider>();

// Feature-flag provider selection (default keeps existing behavior)
// JOB_PROVIDER=adzuna_live to enable live Adzuna ingestion
builder.Services.AddScoped<IJobProvider>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var selected = (cfg["JOB_PROVIDER"] ?? Environment.GetEnvironmentVariable("JOB_PROVIDER") ?? "").Trim();

    if (string.Equals(selected, "adzuna_live", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(selected, "live", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(selected, "adzuna", StringComparison.OrdinalIgnoreCase))
    {
        return sp.GetRequiredService<AdzunaLiveJobProvider>();
    }

    return sp.GetRequiredService<AdzunaJobProvider>();
});

// Register job ingestion service
builder.Services.AddScoped<IJobIngestionService, JobIngestionService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            // Explicit localhost origins (safer than AllowAnyOrigin when auth headers/cookies are involved)
            .WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200",
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Apply migrations automatically on startup (development only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AiJobContext>();
        dbContext.Database.Migrate();

        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        authDb.Database.Migrate();

        await SeedDatabaseAsync(dbContext);
        await SeedIdentityAsync(scope.ServiceProvider);
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must be applied before auth endpoints are hit
app.UseCors("Development");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Seeds the database with sample data if it's empty.
/// </summary>
static async Task SeedDatabaseAsync(AiJobContext context)
{
    if (await context.Skills.AnyAsync())
        return; // Database already seeded

    var skills = new[]
    {
        ".NET", "C#", "Azure", "SQL Server", "Entity Framework",
        "Node.js", "React", "JavaScript", "TypeScript", "Python",
        "Docker", "Kubernetes", "AWS", "GCP", "CI/CD",
        "Machine Learning", "TensorFlow", "PyTorch", "Data Science",
        "Apache Spark", "PostgreSQL", "MongoDB"
    };

    var skillEntities = skills.Select(name => new AiJobMarketIntelligence.Domain.Entities.Skill { Name = name }).ToList();
    context.Skills.AddRange(skillEntities);
    await context.SaveChangesAsync();
}

static async Task SeedIdentityAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var roles = new[] { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Optional: create a default admin based on env vars (so no hard-coded secrets)
    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
