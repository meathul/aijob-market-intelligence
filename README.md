# AI Job Market Intelligence Platform - Starter Project

A production-ready .NET 10 Web API for an AI-powered job market intelligence platform. This starter project implements a complete job ingestion system with a clean architecture following SOLID principles.

## 🎯 Project Overview

This platform provides:
- **Job Ingestion**: Automated fetching and processing of job listings from external APIs
- **Data Management**: Structured storage with relationships between jobs, skills, and processed data
- **RESTful API**: Endpoints to query jobs with filtering and pagination support
- **Background Worker**: Scheduled service for continuous job ingestion
- **Database-First Architecture**: Entity Framework Core with migrations

## 📁 Solution Structure

```
AiJobMarketIntelligence/
├── docs/
├── api/
│   ├── src/
│   │   ├── Domain/                          # Entity models and interfaces
│   │   │   └── Entities/
│   │   │       ├── JobRaw.cs               # Raw job data from external sources
│   │   │       ├── JobProcessed.cs         # Processed job data (after AI analysis)
│   │   │       ├── Skill.cs                # Job skill entities
│   │   │       └── JobSkill.cs             # Join table for job-skill relationship
│   │   ├── Infrastructure/                  # Data access and external services
│   │   │   ├── Data/
│   │   │   │   ├── AiJobContext.cs         # EF Core DbContext
│   │   │   │   └── Migrations/             # Database migrations
│   │   │   └── Repositories/
│   │   ├── Application/                     # Business logic and services
│   │   │   ├── DTOs/                       # Data transfer objects
│   │   │   │   ├── JobRawDto.cs
│   │   │   │   ├── JobProcessedDto.cs
│   │   │   │   ├── JobSkillDto.cs
│   │   │   │   └── JobSearchResultDto.cs
│   │   │   └── Services/
│   │   │       ├── JobIngestionService.cs  # Core ingestion logic
│   │   │       └── Providers/
│   │   │           ├── IJobProvider.cs     # Abstract job provider
│   │   │           └── MockJobProvider.cs  # Mock implementation
│   │   ├── Api/                             # ASP.NET Core Web API
│   │   │   ├── Controllers/
│   │   │   │   ├── JobsController.cs       # Job query endpoints
│   │   │   │   └── AdminController.cs      # Administrative endpoints
│   │   │   ├── Program.cs                  # Configuration and DI setup
│   │   │   └── appsettings.json            # API configuration
│   │   └── Worker/                          # Background job processing service
│   │       ├── Worker.cs                   # JobIngestionWorker implementation
│   │       ├── Program.cs                  # Worker host configuration
│   │       └── appsettings.json            # Worker configuration
│   └── tests/
│       ├── UnitTests/
│       └── IntegrationTests/
├── ui/
│   ├── src/
│   └── tests/
├── build/
├── tools/
├── README.md
└── AiJobMarketIntelligence.slnx
```

## 🗄️ Database Design

### Entities

#### JobRaw
- **Purpose**: Stores raw job listings as ingested from external APIs
- **Fields**: Title, Company, Location, Description, SalaryRaw, Source, Url (UNIQUE), PostedDate, CreatedAt, IsProcessed
- **Relationships**: 1-to-1 with JobProcessed, 1-to-many with JobSkill

#### JobProcessed
- **Purpose**: Stores processed data after AI extraction/normalization
- **Fields**: SalaryMin, SalaryMax, Currency, ExperienceLevel
- **Relationships**: 1-to-1 with JobRaw (FK)

#### Skill
- **Purpose**: Maintains a catalog of unique skills
- **Fields**: Name (UNIQUE)
- **Relationships**: 1-to-many with JobSkill

#### JobSkill
- **Purpose**: Join table for many-to-many relationship
- **Primary Key**: Composite (JobRawId, SkillId)
- **Relationships**: FK to JobRaw, FK to Skill

### Database Constraints
- Unique constraint on `JobRaw.Url` to prevent duplicate entries
- Unique constraint on `Skill.Name` to maintain skill uniqueness
- Cascading deletes on all relationships
- Check constraint on JobSkill IDs to ensure positive values

## 🔧 Tech Stack

- **.NET 10** - Latest LTS framework
- **ASP.NET Core 10** - Web API framework
- **Entity Framework Core 10** - ORM with Code-First approach
- **SQL Server** - Primary database
- **Swagger/Swashbuckle** - API documentation
- **Dependency Injection** - Built-in .NET DI container

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK installed
- SQL Server (local or remote)
- Visual Studio Code or Visual Studio

### Installation

1. **Clone the repository**
```bash
cd /Users/athulkrishnagopakumar/Desktop/Aijob
```

2. **Update connection string** (appsettings.json)
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=AiJobMarketIntelligence;Trusted_Connection=true;TrustServerCertificate=true;"
}
```

3. **Apply database migrations** (run from the **repo root**, not from inside `Api/`)

```bash
cd /Users/athulkrishnagopakumar/project/Aijob

# Option A: helper script (AiJobContext + AuthDbContext)
./scripts/apply-migrations.sh

# Option B: manual (AiJobContext — jobs, preferences, skills)
dotnet ef database update \
  --project api/src/Infrastructure/AiJobMarketIntelligence.Infrastructure/AiJobMarketIntelligence.Infrastructure.csproj \
  --startup-project api/src/Api/AiJobMarketIntelligence.Api/AiJobMarketIntelligence.Api.csproj \
  --context AiJobContext
```

If `dotnet ef` is not installed: `dotnet tool install --global dotnet-ef`

**Common mistake:** from `api/src/Api/AiJobMarketIntelligence.Api`, `--project ../Infrastructure/...` resolves to `api/src/Api/Infrastructure/...` (wrong). Use the full path from repo root above, or `../../Infrastructure/...` from the Api folder.

4. **Run the API**
```bash
dotnet run --project api/src/Api/AiJobMarketIntelligence.Api
```

5. **Run the background worker** (in a separate terminal)
```bash
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker
```

## 📡 API Endpoints

### Jobs Endpoints

#### GET /api/jobs
Retrieve all jobs with pagination

**Query Parameters:**
- `pageNumber` (int, default: 1) - Page number
- `pageSize` (int, default: 20, max: 100) - Items per page

**Response:**
```json
{
  "jobs": [
    {
      "id": 1,
      "title": "Senior Backend Engineer",
      "company": "TechCorp Inc.",
      "location": "San Francisco, CA",
      "description": "...",
      "salaryRaw": "$150,000 - $200,000",
      "source": "MockAPI",
      "url": "https://...",
      "postedDate": "2026-04-11T00:00:00Z",
      "createdAt": "2026-04-13T12:00:00Z",
      "isProcessed": false,
      "skills": []
    }
  ],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

#### GET /api/jobs/{id}
Retrieve a specific job by ID

**Response:**
```json
{
  "id": 1,
  "title": "Senior Backend Engineer",
  "company": "TechCorp Inc.",
  "location": "San Francisco, CA",
  "description": "...",
  "salaryRaw": "$150,000 - $200,000",
  "source": "MockAPI",
  "url": "https://...",
  "postedDate": "2026-04-11T00:00:00Z",
  "createdAt": "2026-04-13T12:00:00Z",
  "isProcessed": false,
  "skills": [
    {
      "skillId": 1,
      "skillName": ".NET"
    }
  ]
}
```

#### GET /api/jobs/search
Search for jobs by keyword and/or location

**Query Parameters:**
- `keyword` (string) - Search in title, description, company
- `location` (string) - Filter by location
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)

**Example:**
```
GET /api/jobs/search?keyword=backend&location=San%20Francisco&pageNumber=1&pageSize=10
```

### Admin Endpoints

#### POST /api/admin/trigger-fetch
Manually trigger the job ingestion process

**Response:**
```json
{
  "success": true,
  "message": "Job ingestion completed successfully",
  "jobsAdded": 5,
  "timestamp": "2026-04-13T12:30:00Z"
}
```

## 🔄 Job Ingestion Flow

1. **Trigger**: Ingestion is triggered either:
   - Automatically every 30 minutes (configurable)
   - Manually via `/api/admin/trigger-fetch` endpoint

2. **Fetch**: `JobIngestionService` calls the configured `IJobProvider`

3. **Validation**: Each job is validated for required fields

4. **Deduplication**: Duplicate URLs are detected and skipped

5. **Storage**: New jobs are saved to `JobsRaw` table

6. **Logging**: All operations are logged with appropriate levels

## 🏗️ Architecture Patterns

### Clean Architecture
- **Domain**: Contains only entities and business logic contracts
- **Application**: Implements business logic and services
- **Infrastructure**: Data access and external integrations
- **API**: Presentation layer with controllers

### Dependency Injection
All services are registered in `Program.cs`:
```csharp
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobIngestionService, JobIngestionService>();
builder.Services.AddScoped<IJobProvider, MockJobProvider>();
```

### Repository Pattern
- Generic interface `IJobRepository` for data access
- Encapsulates EF Core queries
- Enables easy testing and provider swapping

### Background Service
- Runs on a configured interval (30 minutes default)
- Creates scoped DI containers for each execution
- Implements graceful cancellation

## ⚙️ Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AiJobMarketIntelligence;..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "AiJobMarketIntelligence": "Information"
    }
  },
  "JobIngestion": {
    "Provider": "Mock",
    "IntervalMinutes": 30
  }
}
```

### Environment-Specific Settings
- `appsettings.Development.json` - Development logging
- `appsettings.Production.json` - Production settings (create as needed)

## 🧪 Testing the Platform

### 1. Start the API
```bash
dotnet run --project api/src/Api/AiJobMarketIntelligence.Api
```

### 2. Access Swagger UI
Navigate to: `https://localhost:7001/swagger/index.html`

### 3. Test Ingestion Manually
```bash
curl -X POST https://localhost:7001/api/admin/trigger-fetch
```

### 4. Query Jobs
```bash
curl https://localhost:7001/api/jobs?pageNumber=1&pageSize=10
```

### 5. Search Jobs
```bash
curl "https://localhost:7001/api/jobs/search?keyword=backend&location=San%20Francisco"
```

## 📊 Database Seeding

The API automatically seeds the database with common skills on first run:
- .NET, C#, Azure, SQL Server, Entity Framework
- Node.js, React, JavaScript, TypeScript, Python
- Docker, Kubernetes, AWS, GCP, CI/CD
- Machine Learning, TensorFlow, PyTorch
- Apache Spark, PostgreSQL, MongoDB

## 🔐 Security Considerations

**Current State (Development):**
- No authentication implemented
- CORS allows all origins
- SQL Server trusted connection (Windows Auth)

**For Production:**
- Implement JWT authentication
- Add authorization policies
- Restrict CORS to specific origins
- Use SQL Server with strong passwords
- Add rate limiting
- Secure sensitive configuration in Key Vault

## 📝 Logging

Configured with console and debug logging:
- **Information**: Normal operations, job ingestion results
- **Warning**: Skipped jobs, missing data
- **Error**: Exceptions, database errors

Access logs in application console or debug output.

## 🔄 Next Steps for Development

1. **Implement Real API Providers**
   - Create `AdzunaJobProvider` for real job data
   - Implement other job board providers

2. **AI Processing**
   - Create `JobProcessingService` for AI extraction
   - Implement salary parsing
   - Add skill extraction logic

3. **Advanced Features**
   - Job market analytics and dashboards
   - Trend analysis
   - Skill demand tracking
   - Job market reports

4. **Frontend**
   - Web dashboard for job browsing
   - Advanced search and filters
   - Analytics visualization

5. **Testing**
   - Unit tests for services
   - Integration tests for repositories
   - API endpoint testing

## 📚 Project Files

### Migrations
All database migrations are stored in:
`api/src/Infrastructure/AiJobMarketIntelligence.Infrastructure/Data/Migrations/`

To create a new migration:
```bash
dotnet ef migrations add [MigrationName] --project api/src/Infrastructure/AiJobMarketIntelligence.Infrastructure --startup-project api/src/Api/AiJobMarketIntelligence.Api
```

## 🐛 Troubleshooting

**Connection String Errors:**
- Verify SQL Server is running
- Check connection string in appsettings.json
- Ensure database name matches

**Migration Errors:**
- Delete migration files and re-create
- Run `dotnet ef database drop` to reset (development only)

**Build Errors:**
- Run `dotnet restore` to restore dependencies
- Ensure .NET 10 SDK is installed

## 📄 License

This project is provided as-is for development and educational purposes.

## 🤝 Contributing

This is a starter template. Feel free to extend and modify according to your requirements.
