using Microsoft.EntityFrameworkCore;
using AiJobMarketIntelligence.Worker;
using AiJobMarketIntelligence.Infrastructure.Data;
using AiJobMarketIntelligence.Infrastructure.Repositories;
using AiJobMarketIntelligence.Application.Services;
using AiJobMarketIntelligence.Application.Services.Providers;

var builder = Host.CreateApplicationBuilder(args);

// Configure Entity Framework Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AiJobContext>(options =>
    options.UseSqlite(connectionString,
        sqliteOptions => sqliteOptions.MigrationsAssembly("AiJobMarketIntelligence.Infrastructure")));

// Register repositories
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();

// Register Adzuna provider (real job data from free Adzuna API)
builder.Services.AddScoped<AdzunaJobProvider>();
builder.Services.AddScoped<IJobProvider>(sp => sp.GetRequiredService<AdzunaJobProvider>());
builder.Services.AddScoped<IJobIngestionService, JobIngestionService>();

// Add the background job ingestion worker
builder.Services.AddHostedService<JobIngestionWorker>();

// Configure logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var host = builder.Build();
host.Run();
