using Microsoft.EntityFrameworkCore;
using AiJobMarketIntelligence.Infrastructure.Data;
using AiJobMarketIntelligence.Infrastructure.Repositories;
using AiJobMarketIntelligence.Application.Services;
using AiJobMarketIntelligence.Application.Services.Providers;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Interfaces.Services;
using AiJobMarketIntelligence.Application.Services.Salary;
using AiJobMarketIntelligence.Application.Services.Processing;

var builder = WebApplication.CreateBuilder(args);

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

// Register repositories (Infrastructure implementations for Application interfaces)
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IJobProcessedRepository, JobProcessedRepository>();

// Salary parsing + processing pipeline (processing runs in Worker; API reuses services for any ad-hoc processing needs)
builder.Services.AddSingleton<ISalaryParserService, SalaryParserService>();
builder.Services.AddScoped<IJobProcessingService, JobProcessingService>();

// Register job query service
builder.Services.AddScoped<IJobQueryService, JobQueryService>();

// Register Adzuna provider (real job data from free Adzuna API)
builder.Services.AddScoped<AdzunaJobProvider>();
builder.Services.AddScoped<IJobProvider>(sp => sp.GetRequiredService<AdzunaJobProvider>());

// Register job ingestion service
builder.Services.AddScoped<IJobIngestionService, JobIngestionService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            .AllowAnyOrigin()
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
        await SeedDatabaseAsync(dbContext);
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Development");
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
