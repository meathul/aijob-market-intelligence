# Development Guide

## Project Overview

This document provides guidance for developers working on the AI Job Market Intelligence Platform.

## 🏗️ Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────┐
│          API (Controllers)           │  ← HTTP Requests/Responses
├─────────────────────────────────────┤
│       Application (Services)         │  ← Business Logic
├─────────────────────────────────────┤
│ Infrastructure (Repositories, DB)   │  ← Data Access
├─────────────────────────────────────┤
│   Domain (Entities, Interfaces)     │  ← Core Models
└─────────────────────────────────────┘
```

### Dependency Flow
- **Domain** ← No dependencies on other layers
- **Application** ← Depends on Domain
- **Infrastructure** ← Depends on Domain, implements Application interfaces
- **API** ← Depends on Application and Infrastructure

## 📦 Adding a New Feature

### Example: Adding a New External Job Provider

1. **Create Provider Interface** (already exists in `IJobProvider.cs`)

2. **Implement Provider**
   ```csharp
   public class LinkedInJobProvider : IJobProvider
   {
       public async Task<List<JobRaw>> FetchJobsAsync()
       {
           // Implementation
       }
   }
   ```

3. **Register in DI Container** (Program.cs)
   ```csharp
   builder.Services.AddScoped<IJobProvider, LinkedInJobProvider>();
   // Or with factory pattern:
   builder.Services.AddScoped<IJobProvider>(sp => 
       new LinkedInJobProvider(sp.GetRequiredService<HttpClient>(), ...));
   ```

4. **Create/Update Configuration**
   ```json
   "ExternalApis": {
       "LinkedIn": {
           "ApiKey": "your-key",
           "BaseUrl": "https://api.linkedin.com"
       }
   }
   ```

## 🗄️ Database Changes

### Adding a New Entity

1. **Create Entity Class** in `Domain/Entities/`
   ```csharp
   public class JobAnalytic
   {
       public int Id { get; set; }
       public int JobRawId { get; set; }
       public int ViewCount { get; set; }
       // ... other properties
       
       // Navigation
       public JobRaw JobRaw { get; set; }
   }
   ```

2. **Add DbSet to AiJobContext**
   ```csharp
   public DbSet<JobAnalytic> JobAnalytics { get; set; }
   ```

3. **Configure Relationships** in `OnModelCreating`
   ```csharp
   modelBuilder.Entity<JobAnalytic>(entity =>
   {
       entity.HasKey(e => e.Id);
       entity.HasOne(e => e.JobRaw)
           .WithMany()
           .HasForeignKey(e => e.JobRawId);
   });
   ```

4. **Create Migration**
   ```bash
   dotnet ef migrations add AddJobAnalytics \
       --project api/src/Infrastructure/AiJobMarketIntelligence.Infrastructure \
       --startup-project api/src/Api/AiJobMarketIntelligence.Api
   ```

5. **Apply Migration**
   ```bash
   dotnet ef database update \
       --project api/src/Infrastructure/AiJobMarketIntelligence.Infrastructure \
       --startup-project api/src/Api/AiJobMarketIntelligence.Api
   ```

## 🧪 Testing Approach

### Unit Testing Services

Create test class:
```csharp
[TestClass]
public class JobIngestionServiceTests
{
    private Mock<IJobRepository> _mockRepository;
    private Mock<IJobProvider> _mockProvider;
    private JobIngestionService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IJobRepository>();
        _mockProvider = new Mock<IJobProvider>();
        _service = new JobIngestionService(_mockRepository.Object, _mockProvider.Object, /* logger */);
    }

    [TestMethod]
    public async Task IngestJobsAsync_WithNewJobs_SavesSuccessfully()
    {
        // Arrange
        var testJobs = new List<JobRaw> { /* test data */ };
        _mockProvider.Setup(p => p.FetchJobsAsync()).ReturnsAsync(testJobs);

        // Act
        var result = await _service.IngestJobsAsync();

        // Assert
        Assert.AreEqual(testJobs.Count, result);
        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<JobRaw>>()), Times.Once);
    }
}
```

### Integration Testing

Test with real database:
```csharp
[TestClass]
public class JobRepositoryTests
{
    private AiJobContext _context;
    private JobRepository _repository;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AiJobContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AiJobContext(options);
        _repository = new JobRepository(_context);
    }

    [TestMethod]
    public async Task GetByIdAsync_WithValidId_ReturnsJob()
    {
        // Arrange
        var job = new JobRaw { Title = "Test", /* ... */ };
        _context.JobsRaw.Add(job);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByIdAsync(job.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Title);
    }
}
```

## 🔍 Code Quality Standards

### Naming Conventions
- **Classes**: PascalCase (`JobIngestionService`)
- **Methods**: PascalCase (`FetchJobsAsync`)
- **Properties**: PascalCase (`JobRawId`)
- **Private fields**: camelCase (`_jobRepository`)
- **Local variables**: camelCase (`jobCount`)
- **Constants**: UPPER_SNAKE_CASE (`MAX_PAGE_SIZE`)

### Async/Await Patterns
- Always use `async/await` for I/O operations
- Method names ending with `Async` return `Task` or `Task<T>`
- Use `await` instead of `.Result` or `.Wait()`

```csharp
// ✅ Good
public async Task<List<JobRaw>> GetJobsAsync()
{
    return await _repository.GetAllAsync();
}

// ❌ Bad
public List<JobRaw> GetJobs()
{
    return _repository.GetAllAsync().Result;
}
```

### Error Handling
- Log all exceptions with context
- Return appropriate HTTP status codes
- Don't expose internal errors to clients

```csharp
try
{
    // Operation
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database error occurred");
    throw; // Or handle gracefully
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    throw;
}
```

## 📊 Configuration Management

### Development Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AiJobMarketIntelligence;Trusted_Connection=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

### Production Configuration
- Use environment variables or Azure Key Vault
- Never commit secrets
- Use connection string builder for sensitive data

```csharp
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
```

## 🔄 Git Workflow

### Branch Naming
- `feature/feature-name` - New features
- `bugfix/bug-name` - Bug fixes
- `hotfix/issue-name` - Production hotfixes

### Commit Messages
```
[TYPE] Short description

Detailed explanation if needed.

Closes #ISSUE_NUMBER
```

Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`

## 🚀 Deployment

### Publishing
```bash
dotnet publish -c Release -o ./publish
```

### Docker (Optional)
Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
RUN dotnet build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80 443
ENTRYPOINT ["dotnet", "AiJobMarketIntelligence.Api.dll"]
```

## 📚 Useful Commands

### EF Core Commands
```bash
# Create migration
dotnet ef migrations add MigrationName --project api/src/Infrastructure --startup-project api/src/Api

# Update database
dotnet ef database update --project api/src/Infrastructure --startup-project api/src/Api

# Remove last migration
dotnet ef migrations remove --project api/src/Infrastructure --startup-project api/src/Api

# Generate SQL script
dotnet ef migrations script --project api/src/Infrastructure --startup-project api/src/Api -o script.sql
```

### Build & Run
```bash
# Build solution
dotnet build

# Run API
dotnet run --project api/src/Api/AiJobMarketIntelligence.Api

# Run Worker
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker

# Run tests
dotnet test
```

## 🐛 Debugging

### In Visual Studio Code
Add `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/api/src/Api/AiJobMarketIntelligence.Api/bin/Debug/net10.0/AiJobMarketIntelligence.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "console": "internalConsole"
    }
  ]
}
```

### Logging in Development
All loggers automatically log to console. Use:
```csharp
_logger.LogInformation("Job ingestion started");
_logger.LogDebug("Processing job: {JobId}", jobId);
_logger.LogError(ex, "Error occurred");
```

## 📖 Further Learning

- [Microsoft .NET Documentation](https://docs.microsoft.com/dotnet)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
