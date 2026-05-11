# Testing & Populating JobSkills Table

## Overview

This guide walks you through testing the OpenAI ChatGPT skill extraction and populating the `JobSkills` table with extracted skills.

## Prerequisites

1. ✅ OpenAI API key (from https://platform.openai.com/account/api-keys)
2. ✅ MySQL database running (AiJobMarketIntelligence)
3. ✅ .env file configured with your API key
4. ✅ .NET 8.0.420 SDK installed

## Quick Test (5 minutes)

### Step 1: Configure Your API Key

Edit `.env` file in project root:
```bash
nano .env
```

Add your OpenAI API key:
```
OPENAI_API_KEY=sk-your-actual-api-key-here
```

### Step 2: Start the Worker

The Worker automatically:
1. Ingests jobs (every 30 minutes)
2. Processes jobs (every 15 minutes)
3. Extracts skills using ChatGPT
4. Saves to JobSkills table

```bash
cd /Users/athulkrishnagopakumar/project/Aijob
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

### Step 3: Monitor the Logs

Look for these success messages:

```
[INF] Calling OpenAI ChatGPT for skill extraction
[INF] Extracted 10 skills: C#, React, Azure, Docker, SQL Server, ...
[INF] Skill 'C#' extracted for job 1
[INF] Skill 'React' extracted for job 1
...
```

### Step 4: Verify Database Population

In a separate terminal, check the JobSkills table:

```bash
# Connect to MySQL
mysql -u root -p AiJobMarketIntelligence

# Query the JobSkills table
SELECT js.Id, jr.Title, s.Name 
FROM JobSkills js
JOIN JobsRaw jr ON js.JobRawId = jr.Id
JOIN Skills s ON js.SkillId = s.Id
LIMIT 20;
```

Expected output:
```
+----+------------------------------------------+------------+
| Id | Title                                    | Name       |
+----+------------------------------------------+------------+
|  1 | Senior Full Stack Engineer               | C#         |
|  2 | Senior Full Stack Engineer               | React      |
|  3 | Senior Full Stack Engineer               | Azure      |
|  4 | Senior Full Stack Engineer               | Docker     |
|  5 | Senior Full Stack Engineer               | SQL Server |
|  6 | Backend Engineer                         | Python     |
|  7 | Backend Engineer                         | Django     |
|  8 | Backend Engineer                         | PostgreSQL |
... and more
+----+------------------------------------------+------------+
```

## Detailed Testing Steps

### Test 1: Check Job Ingestion

Before skill extraction can happen, jobs must be ingested.

```bash
# Check JobsRaw table
mysql -u root -p AiJobMarketIntelligence
SELECT COUNT(*) as TotalJobs FROM JobsRaw;
```

Expected: Should show number of ingested jobs (demo has 23+)

```sql
SELECT Id, Title, Source FROM JobsRaw LIMIT 5;
```

Expected output:
```
+----+------------------------------------------+--------+
| Id | Title                                    | Source |
+----+------------------------------------------+--------+
|  1 | Senior Full Stack Engineer               | Demo   |
|  2 | Backend Engineer                         | Demo   |
|  3 | Frontend Developer                       | Demo   |
... more jobs
```

### Test 2: Check Job Processing

Jobs must be processed before skills are extracted.

```bash
# Check JobsProcessed table
SELECT COUNT(*) as ProcessedJobs FROM JobsProcessed;
```

Expected: Should match or be less than JobsRaw count

```sql
SELECT Id, JobRawId, SalaryMin, SalaryMax, ExperienceLevel, ProcessedAt 
FROM JobsProcessed 
LIMIT 5;
```

Expected output:
```
+----+---------+----------+----------+----------------+---------------------+
| Id | JobRawId| SalaryMin| SalaryMax| ExperienceLevel| ProcessedAt         |
+----+---------+----------+----------+----------------+---------------------+
|  1 |       1 |    120000|    180000| Senior         | 2026-05-11 12:00:00 |
|  2 |       2 |     90000|    150000| Mid            | 2026-05-11 12:00:00 |
|  3 |       3 |     80000|    120000| Mid            | 2026-05-11 12:00:00 |
... more processed jobs
```

### Test 3: Check Skills Extraction

After processing, skills should be extracted.

```bash
# Count extracted skills
SELECT COUNT(*) as TotalExtractedSkills FROM JobSkills;
```

Expected: > 0 (should have extracted skills)

```sql
# See skills by job
SELECT 
    jr.Id as JobId,
    jr.Title,
    COUNT(js.Id) as SkillCount,
    GROUP_CONCAT(s.Name SEPARATOR ', ') as Skills
FROM JobSkills js
JOIN JobsRaw jr ON js.JobRawId = jr.Id
JOIN Skills s ON js.SkillId = s.Id
GROUP BY jr.Id, jr.Title
LIMIT 10;
```

Expected output:
```
+-------+------------------------------------------+----------+-------------------------------------------+
| JobId | Title                                    | Count    | Skills                                    |
+-------+------------------------------------------+----------+-------------------------------------------+
|     1 | Senior Full Stack Engineer               |        9 | C#, React, Azure, Docker, SQL Server,... |
|     2 | Backend Engineer                         |        6 | Python, Django, PostgreSQL, REST API,... |
|     3 | Frontend Developer                       |        7 | React, TypeScript, CSS, HTML, Jest,...   |
... more jobs with skills
```

## End-to-End Test Workflow

### Complete Flow (Start to Finish)

```bash
# Terminal 1: Start the Worker
cd /Users/athulkrishnagopakumar/project/Aijob
export OPENAI_API_KEY="sk-your-api-key"
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/

# Watch logs for:
# [INF] Ingesting jobs...
# [INF] Processing jobs...
# [INF] Extracted X skills: ...
```

```bash
# Terminal 2: Monitor Database (run simultaneously)
mysql -u root -p AiJobMarketIntelligence

# Keep checking these queries:
SELECT COUNT(*) FROM JobsRaw;
SELECT COUNT(*) FROM JobsProcessed;
SELECT COUNT(*) FROM JobSkills;

# Watch the counts increase!
```

## Timing Guide

The Worker runs on schedules:
- **Job Ingestion**: Every 30 minutes
- **Job Processing** (salary parsing + skill extraction): Every 15 minutes

To see results faster:

### Option 1: Wait for Natural Schedule
- Ingestion: Wait up to 30 minutes
- Processing: Wait up to 15 minutes after ingestion
- Total: 45 minutes worst case

### Option 2: Manual Trigger (Advanced)
If you want to test immediately without waiting, you can modify the Worker's scheduled times in `Program.cs`:

```csharp
// Current: Every 30 min for ingestion, every 15 min for processing
builder.Services.AddHostedService<JobIngestionWorker>();  // 30 min interval
builder.Services.AddHostedService<JobProcessingWorker>();  // 15 min interval

// For testing: Change to every 10 seconds
// See: api/src/Worker/AiJobMarketIntelligence.Worker/JobIngestionWorker.cs
// and: api/src/Worker/AiJobMarketIntelligence.Worker/JobProcessingWorker.cs
```

## Expected Results

### After 1st Run

```
JobsRaw:        23 jobs (ingested)
JobsProcessed:  23 jobs (processed, salaries parsed)
JobSkills:      150-200 skills (extracted)
Skills:         100-150 unique skills (created)
```

### Sample Skills Extracted

The system should extract a variety of skills:

**Programming Languages:**
- C#, Python, JavaScript, TypeScript, Java, Go, Rust

**Frontend:**
- React, Angular, Vue, Next.js, HTML, CSS, Tailwind

**Backend:**
- .NET, Django, Spring, Node.js, FastAPI, Laravel

**Databases:**
- MySQL, PostgreSQL, MongoDB, SQL Server, Redis, DynamoDB

**Cloud/DevOps:**
- AWS, Azure, Google Cloud, Docker, Kubernetes, CI/CD

**Testing/Tools:**
- xUnit, Jest, Pytest, Git, GitHub, Agile, Scrum

## Troubleshooting

### No Skills Being Extracted

**Symptom:** JobSkills table remains empty

**Checklist:**
1. ✅ Verify API key in .env file
   ```bash
   cat .env | grep OPENAI_API_KEY
   ```

2. ✅ Check Worker logs for errors
   ```
   [ERR] Error extracting skills: Unauthorized
   ```

3. ✅ Verify API key is valid
   - Visit: https://platform.openai.com/account/api-keys
   - Check if key is still active

4. ✅ Check if jobs are being processed
   ```bash
   mysql -u root -p AiJobMarketIntelligence
   SELECT COUNT(*) FROM JobsProcessed;
   ```

5. ✅ Check application logs for ChatGPT errors
   ```
   [ERR] Error extracting skills from job description
   ```

**Solutions:**
- Get a fresh API key from OpenAI
- Check API rate limits (might need to wait)
- Verify internet connection to OpenAI API
- Check firewall/proxy settings

### Database Errors

**Symptom:** "Cannot connect to database"

```bash
# Test MySQL connection
mysql -u root -p -h 127.0.0.1 AiJobMarketIntelligence

# Check connection string in appsettings.json
cat api/src/Worker/AiJobMarketIntelligence.Worker/appsettings.json
```

### Duplicate Skills

The system prevents duplicate job-skill associations:

```sql
-- This is normal - prevents adding same skill twice
SELECT JobRawId, SkillId, COUNT(*) 
FROM JobSkills 
GROUP BY JobRawId, SkillId 
HAVING COUNT(*) > 1;

-- Should return empty (no duplicates)
```

## Sample Test Queries

### Query 1: Skills Count by Job

```sql
SELECT 
    jr.Title,
    COUNT(DISTINCT js.SkillId) as UniqueSkills
FROM JobSkills js
JOIN JobsRaw jr ON js.JobRawId = jr.Id
GROUP BY jr.Title
ORDER BY UniqueSkills DESC
LIMIT 10;
```

### Query 2: Most Common Skills

```sql
SELECT 
    s.Name,
    COUNT(js.Id) as JobCount
FROM JobSkills js
JOIN Skills s ON js.SkillId = s.Id
GROUP BY s.Name
ORDER BY JobCount DESC
LIMIT 20;
```

Output might show:
```
+------------------+----------+
| Name             | JobCount |
+------------------+----------+
| REST API         |       18 |
| SQL              |       17 |
| Cloud            |       16 |
| Git              |       15 |
| Agile            |       14 |
| Docker           |       13 |
| AWS              |       12 |
| JSON             |       12 |
| Kubernetes       |       11 |
| Azure            |       10 |
```

### Query 3: Skills by Category

```sql
-- You could categorize skills like this:
SELECT 
    CASE 
        WHEN s.Name IN ('C#', 'Python', 'Java', 'JavaScript', 'TypeScript') 
            THEN 'Programming Language'
        WHEN s.Name IN ('React', 'Angular', 'Vue', 'Next.js') 
            THEN 'Frontend Framework'
        WHEN s.Name IN ('Django', 'Spring', '.NET', 'Node.js') 
            THEN 'Backend Framework'
        ELSE 'Other'
    END as Category,
    s.Name,
    COUNT(js.Id) as JobCount
FROM JobSkills js
JOIN Skills s ON js.SkillId = s.Id
GROUP BY Category, s.Name
ORDER BY Category, JobCount DESC;
```

## Test Results Summary

When testing is complete, you should have:

✅ **23+ jobs ingested** (JobsRaw)
✅ **23+ jobs processed** (JobsProcessed with salary/experience)
✅ **150-200+ job-skill associations** (JobSkills)
✅ **100-150+ unique skills** (Skills)
✅ **No null values** (all required fields populated)
✅ **No duplicate associations** (unique constraint working)

## Performance Notes

- **Skill extraction per job**: ~1-3 seconds (ChatGPT API latency)
- **Database write time**: ~100ms per skill
- **Total time for 23 jobs**: ~3-5 minutes
- **Network**: Requires internet connection to OpenAI API

## Next Steps

After populating JobSkills table:

1. **Run the API** to query skills
   ```bash
   dotnet run --project api/src/Api/AiJobMarketIntelligence.Api/
   ```

2. **Access Swagger UI** at http://localhost:5062/swagger
   - Test skill search endpoints
   - Query jobs by skill
   - Get job details with skills

3. **Build analytics** on extracted skills
   - Most in-demand skills
   - Skills by job level
   - Technology trends

4. **Monitor performance** and costs
   - Track API calls
   - Monitor ChatGPT costs
   - Optimize extraction

## Cost Estimation

For populating 23 jobs:
- 23 jobs × ~200 tokens per job = ~4,600 tokens input
- Response tokens: ~2,000 tokens
- Cost: ~$0.0025 USD (very cheap!)

Larger datasets:
- 1,000 jobs: ~$0.10
- 10,000 jobs: ~$1.00
- 100,000 jobs: ~$10.00

---

**Ready to test?** Start with Step 1 and follow the Quick Test guide above! 🚀
