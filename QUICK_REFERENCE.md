# Quick Reference Card

## 🚀 Quick Start (5 Minutes)

```bash
# 1. Navigate to project
cd /Users/athulkrishnagopakumar/Desktop/Aijob

# 2. Update connection string (if needed)
# Edit: src/Api/AiJobMarketIntelligence.Api/appsettings.json

# 3. Apply database migration
dotnet ef database update \
  --project src/Infrastructure/AiJobMarketIntelligence.Infrastructure \
  --startup-project src/Api/AiJobMarketIntelligence.Api

# 4. Run API
dotnet run --project src/Api/AiJobMarketIntelligence.Api

# 5. Open browser
# https://localhost:7001/swagger
```

## 📁 Project Structure

```
src/
├── Domain/              → Entities (JobRaw, JobProcessed, Skill, JobSkill)
├── Infrastructure/      → DbContext, Repositories, Migrations
├── Application/         → Services, DTOs, Providers
├── Api/                → Controllers, Program.cs
└── Worker/             → Background service
```

## 🔌 Key Classes

| Class | Purpose | File |
|-------|---------|------|
| `AiJobContext` | EF Core DbContext | Infrastructure/Data/AiJobContext.cs |
| `JobRepository` | Job data access | Infrastructure/Repositories/IJobRepository.cs |
| `JobIngestionService` | Core ingestion logic | Application/Services/JobIngestionService.cs |
| `MockJobProvider` | Mock external API | Application/Services/Providers/MockJobProvider.cs |
| `JobsController` | API endpoints | Api/Controllers/JobsController.cs |
| `JobIngestionWorker` | Background service | Worker/Worker.cs |

## 🌐 API Endpoints

```
GET    /api/jobs                    → List jobs (paginated)
GET    /api/jobs/{id}              → Get single job
GET    /api/jobs/search            → Search by keyword/location
POST   /api/admin/trigger-fetch    → Manually trigger ingestion
GET    /swagger                    → API documentation
```

## 📊 Database Schema

```
JobsRaw (PK: Id, UNIQUE: Url)
  ├─→ JobsProcessed (FK: JobRawId)
  └─→ JobSkills (FK: JobRawId)
      └─→ Skills (FK: SkillId, UNIQUE: Name)
```

## 🔧 Common Tasks

### Run Tests
```bash
dotnet test
```

### Create New Migration
```bash
dotnet ef migrations add [Name] \
  --project src/Infrastructure \
  --startup-project src/Api
```

### Apply Migration
```bash
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Api
```

### Run Worker
```bash
dotnet run --project src/Worker/AiJobMarketIntelligence.Worker
```

### View Logs
```bash
# API logs appear in console when running:
dotnet run --project src/Api/AiJobMarketIntelligence.Api
```

### Docker Build
```bash
docker-compose build
docker-compose up
```

## 📝 Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Main configuration |
| `appsettings.Development.json` | Dev logging |
| `.env` | Environment variables (local) |

## 🔑 Connection String Locations

- **API**: `src/Api/AiJobMarketIntelligence.Api/appsettings.json`
- **Worker**: `src/Worker/AiJobMarketIntelligence.Worker/appsettings.json`

## 📚 Documentation

| Document | Content |
|----------|---------|
| `README.md` | Project overview, API docs, setup |
| `DEVELOPMENT.md` | Architecture, testing, coding standards |
| `INSTALLATION.md` | Detailed setup, Docker, Azure |
| `PROJECT_SUMMARY.md` | Delivery details, what's included |

## 🐛 Common Issues

| Issue | Solution |
|-------|----------|
| Connection timeout | Check SQL Server running, verify connection string |
| Port 5001 in use | `kill -9 $(lsof -t -i :5001)` (macOS/Linux) |
| Migration fails | `dotnet ef migrations remove` then recreate |
| Build errors | `dotnet restore` then `dotnet build` |

## 🎯 Next Development Steps

1. **Add Authentication** - Implement JWT tokens
2. **Real Providers** - Integrate with LinkedIn, Indeed APIs
3. **AI Processing** - Add salary parsing, skill extraction
4. **Frontend** - Build web dashboard
5. **Testing** - Add unit and integration tests

## 🔐 Security Checklist

- [ ] Change default connection string password
- [ ] Add API authentication (JWT)
- [ ] Configure HTTPS certificates
- [ ] Add input validation
- [ ] Set up rate limiting
- [ ] Enable CORS restrictions
- [ ] Use environment variables for secrets

## 💾 Database Commands

```bash
# Create database backup (SQL Server)
BACKUP DATABASE AiJobMarketIntelligence 
TO DISK = 'C:\backup\db.bak'

# Reset database (WARNING: deletes all data)
dotnet ef database drop \
  --project src/Infrastructure \
  --startup-project src/Api

# View current migrations
dotnet ef migrations list \
  --project src/Infrastructure
```

## 📊 Useful API Queries

```bash
# Get all jobs (page 1)
curl https://localhost:7001/api/jobs

# Get single job
curl https://localhost:7001/api/jobs/1

# Search backend jobs in NYC
curl "https://localhost:7001/api/jobs/search?keyword=backend&location=New%20York"

# Trigger ingestion
curl -X POST https://localhost:7001/api/admin/trigger-fetch

# Pagination example
curl "https://localhost:7001/api/jobs?pageNumber=2&pageSize=50"
```

## 🔄 Deployment Quick Links

- **Local**: Run `dotnet run`
- **Docker**: Run `docker-compose up`
- **Azure**: Follow INSTALLATION.md → Azure Deployment
- **AWS**: Deploy to EC2 or App Runner

## 📞 File Locations

| Item | Location |
|------|----------|
| API Controllers | `src/Api/AiJobMarketIntelligence.Api/Controllers/` |
| Entities | `src/Domain/AiJobMarketIntelligence.Domain/Entities/` |
| Repositories | `src/Infrastructure/.../Repositories/` |
| Services | `src/Application/.../Services/` |
| Migrations | `src/Infrastructure/.../Data/Migrations/` |
| Configuration | `src/{Api,Worker}/.../appsettings.json` |

## ⚡ Performance Tips

1. Use pagination: `?pageNumber=1&pageSize=50`
2. Index frequently queried columns (already set up)
3. Use async/await (already implemented)
4. Consider caching for skills list
5. Monitor database queries

## 🎓 Learning Path

1. Read `README.md` - Understand project
2. Read `DEVELOPMENT.md` - Learn architecture
3. Explore `src/Domain/` - Understand entities
4. Explore `src/Infrastructure/` - Learn data access
5. Explore `src/Application/` - Understand services
6. Explore `src/Api/` - See API structure
7. Modify code and extend features

---

**Everything is ready to go! Start with step 1 in Quick Start above.** ✅
