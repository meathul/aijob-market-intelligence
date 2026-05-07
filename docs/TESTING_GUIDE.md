# Testing Guide - AI Job Market Intelligence Platform

## Quick Start

### Start the API
```bash
cd /Users/athulkrishnagopakumar/project/Aijob
dotnet run --project api/src/Api/AiJobMarketIntelligence.Api
```

The API will be available at `http://localhost:5062`

### Trigger Job Fetch
```bash
curl -X POST http://localhost:5062/api/admin/trigger-fetch
```

### Get All Jobs
```bash
curl http://localhost:5062/api/jobs
```

### Get Paginated Jobs
```bash
curl "http://localhost:5062/api/jobs?pageNumber=1&pageSize=10"
```

### Search Jobs
```bash
curl "http://localhost:5062/api/jobs/search?query=Developer"
```

### Get Single Job
```bash
curl http://localhost:5062/api/jobs/1
```

---

## How the System Works

### Data Sources

The system uses a multi-source approach:

1. **Remotive API** (Primary)
   - URL: `https://remotive.io/api/remote-jobs`
   - Returns: Up to 50 remote developer jobs
   - Auth: None required
   - Status: May fail due to network restrictions in some environments

2. **GitHub Jobs API** (Fallback)
   - URL: `https://api.github.com/jobs?description=developer`
   - Returns: Up to 30 developer jobs (API is deprecated but still works)
   - Auth: None required
   - Status: May return limited results

3. **Demo/Test Data** (Development Fallback)
   - 23 sample tech job listings
   - Used when external APIs are unavailable
   - Clearly marked with `"source": "Sample"` in responses
   - Useful for testing in environments with network restrictions

### Data Flow

```
API/Worker
    ↓
JobIngestionService (Orchestration)
    ↓
AdzunaJobProvider (Multi-source fetching)
    ├─→ Try Remotive API
    ├─→ If < 10 jobs, try GitHub Jobs API
    └─→ If < 10 jobs, load demo/test data
    ↓
URL Deduplication
    ↓
JobRepository (SQLite Database)
    ↓
Response to Client
```

### API Endpoints

#### 1. Get All Jobs
- **URL**: `GET /api/jobs`
- **Query Params**: 
  - `pageNumber` (default: 1)
  - `pageSize` (default: 20, max: 100)
- **Response**: Paginated job list

#### 2. Get Single Job
- **URL**: `GET /api/jobs/{id}`
- **Response**: Single job with details

#### 3. Search Jobs
- **URL**: `GET /api/jobs/search`
- **Query Params**: `query` (searches title, company, description)
- **Response**: Matching jobs

#### 4. Trigger Manual Fetch
- **URL**: `POST /api/admin/trigger-fetch`
- **Response**: Ingestion result with job count

---

## Data Storage

**Database**: SQLite (File-based)
- **Location**: `api/src/Api/AiJobMarketIntelligence.Api/AiJobMarketIntelligence.db`
- **No setup required** - database is automatically created on first run
- **Schema**: 4 tables (JobRaw, JobProcessed, Skill, JobSkill)

---

## Background Worker

The system includes an independent background worker that automatically fetches jobs every 30 minutes:

```bash
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker
```

**Features**:
- Runs independently from the API
- Scheduled to fetch every 30 minutes
- Same multi-source logic as manual fetch
- Full logging and error handling

---

## Environment Support

### Real API Data (Requires Internet Access)
- ✅ Remotive jobs will populate if accessible
- ✅ GitHub Jobs will populate if Remotive fails
- ✅ Real, authenticated data with no hardcoding

### Network-Restricted Environments
- ✅ Demo/test data automatically loads as fallback
- ✅ System remains fully functional for testing
- ✅ All 23 sample jobs clearly marked with `"source": "Sample"`
- ✅ No hidden hardcoding - transparent fallback with logging

---

## Sample Response

```json
{
  "jobs": [
    {
      "id": 1,
      "title": "Senior Software Engineer - .NET/C#",
      "company": "TechCorp Solutions",
      "location": "San Francisco, CA",
      "description": "We're looking for an experienced .NET engineer...",
      "salaryRaw": "$150,000 - $200,000 per year",
      "source": "Sample",
      "url": "https://techcorp.example.com/jobs/senior-dotnet-engineer",
      "postedDate": "2026-04-15T04:44:34.763926",
      "createdAt": "2026-04-17T04:44:34",
      "isProcessed": false,
      "skills": []
    }
  ],
  "totalCount": 23,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 2
}
```

---

## Future Enhancements

- [ ] AI Processing Layer: Salary extraction, experience level detection
- [ ] Skill Tagging: Automatic skill extraction from job descriptions
- [ ] Multiple API Sources: Indeed, LinkedIn, Stack Overflow, etc.
- [ ] Frontend Dashboard: React/Vue UI for job browsing
- [ ] Advanced Filtering: By salary, skills, location, experience level
- [ ] Job Notifications: Email alerts for matching jobs

---

## Troubleshooting

### No jobs appear after fetch
1. Check if external APIs are accessible: `curl https://remotive.io/api/remote-jobs`
2. If blocked, system will use demo data (this is expected)
3. Verify database file exists: `ls api/src/Api/AiJobMarketIntelligence.Api/AiJobMarketIntelligence.db`

### Port already in use
```bash
# Find and kill process on port 5062
lsof -i :5062
kill -9 <PID>
```

### Build errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

---

## Architecture Notes

**Clean Architecture Layers**:
1. **Domain** - Job entities and skill catalog
2. **Infrastructure** - Database access and repositories
3. **Application** - Job fetching and ingestion logic
4. **API** - REST endpoints and controllers
5. **Worker** - Background scheduling service

**Key Design Patterns**:
- Repository Pattern: Abstracted data access
- Dependency Injection: Built-in .NET container
- Async/Await: 100% non-blocking code
- Multi-source fallback: Resilient to API failures
