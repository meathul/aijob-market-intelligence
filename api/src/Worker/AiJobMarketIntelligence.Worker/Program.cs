using Microsoft.EntityFrameworkCore;
using AiJobMarketIntelligence.Worker;
using AiJobMarketIntelligence.Infrastructure.Data;
using AiJobMarketIntelligence.Infrastructure.Repositories;
using AiJobMarketIntelligence.Application.Services;
using AiJobMarketIntelligence.Application.Services.Providers;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Services.Salary;
using AiJobMarketIntelligence.Application.Services.Processing;

var builder = Host.CreateApplicationBuilder(args);

// Configure Entity Framework Core with MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
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

// Salary parsing + processing pipeline
builder.Services.AddSingleton<ISalaryParserService, SalaryParserService>();
builder.Services.AddScoped<IJobProcessingService, JobProcessingService>();

// Register Adzuna provider (real job data from free Adzuna API)
builder.Services.AddScoped<AdzunaJobProvider>();
builder.Services.AddScoped<IJobProvider>(sp => sp.GetRequiredService<AdzunaJobProvider>());

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
