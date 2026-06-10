using Microsoft.EntityFrameworkCore;
// Remove API bootstrap reference
// using AiJobMarketIntelligence.Api.Services;
using AiJobMarketIntelligence.Worker;
using AiJobMarketIntelligence.Infrastructure.Data;
using AiJobMarketIntelligence.Infrastructure.Repositories;
using AiJobMarketIntelligence.Application.Services;
using AiJobMarketIntelligence.Application.Services.Providers;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Services.Salary;
using AiJobMarketIntelligence.Application.Services.Skills;
using AiJobMarketIntelligence.Application.Services.Processing;

var builder = Host.CreateApplicationBuilder(args);

// Load environment variables from .env (search upward from output directory/repo)
DotEnvBootstrap.LoadFromRepoRoot(AppContext.BaseDirectory);

// Make sure environment variables are part of configuration consistently.
builder.Configuration.AddEnvironmentVariables();

// Minimal startup diagnostics (does not print secrets)
{
    var dotenvMissing = Environment.GetEnvironmentVariable("AIJOB_DOTENV_NOT_FOUND") == "1";
    var apiKeyPresent = !string.IsNullOrWhiteSpace(
        Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? builder.Configuration["GROQ_API_KEY"] ?? builder.Configuration["OPENAI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

    if (dotenvMissing)
        builder.Logging.AddConsole();

    var logger = LoggerFactory.Create(lb => lb.AddConsole()).CreateLogger("WorkerStartup");
    logger.LogInformation(".env loaded: {DotEnvLoaded}; API_KEY present: {OpenAiKeyPresent}", !dotenvMissing, apiKeyPresent);
}

// Configure Entity Framework Core with MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? builder.Configuration["ConnectionStrings__DefaultConnection"]
    ?? builder.Configuration["DB_CONNECTION_STRING"]
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AiJobContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.MigrationsAssembly("AiJobMarketIntelligence.Infrastructure")));

// Register repositories (Infrastructure implementations for Application interfaces)
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IJobProcessedRepository, JobProcessedRepository>();

// Salary parsing + skill extraction + processing pipeline
builder.Services.AddSingleton<ISalaryParserService, SalaryParserService>();

// Register Groq/OpenAI skill extraction service
var openAiApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
    ?? builder.Configuration["GROQ_API_KEY"]
    ?? builder.Configuration["Groq:ApiKey"]
    ?? builder.Configuration["OpenAI:ApiKey"]
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? builder.Configuration["OPENAI_API_KEY"]
    ?? throw new InvalidOperationException("API key is required. Set it via GROQ_API_KEY or OPENAI_API_KEY environment variable.");

builder.Services.AddSingleton<ISkillExtractionService>(sp =>
    new OpenAiSkillExtractionService(openAiApiKey, sp.GetRequiredService<ILogger<OpenAiSkillExtractionService>>()));

builder.Services.AddScoped<IJobProcessingService, JobProcessingService>();

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

// Add the background job ingestion worker
builder.Services.AddHostedService<JobIngestionWorker>();

// Add the background job processing worker
builder.Services.AddHostedService<JobProcessingWorker>();

// Configure logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var host = builder.Build();
host.Run();
