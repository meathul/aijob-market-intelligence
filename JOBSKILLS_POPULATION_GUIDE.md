# JobSkills Table Population & Testing Guide

## Quick Start: Run the Worker

```bash
# 1. Navigate to project directory
cd /Users/athulkrishnagopakumar/project/Aijob

# 2. Edit .env and add your OpenAI API key
nano .env
# Add: OPENAI_API_KEY=sk-your-real-api-key

# 3. Run the Worker
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

The worker will automatically:
1. Ingest jobs (every 30 minutes)
2. Process jobs (every 15 minutes)
3. Extract skills using ChatGPT
4. Populate the JobSkills table

## What Happens When You Run the Worker

```
[INF] AiJobMarketIntelligence.Worker.JobIngestionWorker
      Ingesting jobs from demo provider...
      23 jobs ingested successfully

[INF] AiJobMarketIntelligence.Worker.JobProcessingWorker
      Processing pending jobs...
      Processing job 1: "Senior Developer"
      Parsing salary: $100k - $120k/year
      Calling OpenAI ChatGPT for skill extraction
      Extracted 12 skills: C#, .NET, React, Azure, SQL Server, ...
      Saved job and skills to database
      ✓ Job 1 processed successfully
      
      [Continue for all 23 jobs...]
      
      23/23 jobs processed
      ✓ All jobs processed successfully
```

## Monitoring the Worker

### 1. Watch Logs for Skill Extraction
Look for messages like:
```
[INF] Extracted 12 skills: C#, React, Azure, Docker, ...
[INF] Skill 'C#' extracted for job 1
[INF] Skill 'React' extracted for job 1
...
```

### 2. Query the Database While Running

**Terminal 1:** Run the Worker
```bash
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

**Terminal 2:** Monitor the database
```bash
# Connect to MySQL
mysql -h 127.0.0.1 -u root -p AiJobMarketIntelligence

# Check JobSkills population
SELECT COUNT(*) as TotalJobSkills FROM JobSkills;
-- Should show increasing numbers as jobs are processed

# See extracted skills for a job
SELECT s.Name, COUNT(*) as Count
FROM JobSkills js
JOIN Skills s ON js.SkillId = s.Id
GROUP BY s.Name
ORDER BY Count DESC
LIMIT 20;

# See which jobs have skills extracted
SELECT j.Id, j.Title, COUNT(js.Id) as SkillCount
FROM JobsRaw j
LEFT JOIN JobSkills js ON j.Id = js.JobRawId
GROUP BY j.Id, j.Title
ORDER BY SkillCount DESC;
```

### 3. Check Application Logs

The logs show detailed information:
```
[DBG] Processing job: "Senior Full Stack Engineer"
[DBG] Salary parsed: Min=100000, Max=120000, Period=Yearly
[INF] Calling OpenAI ChatGPT for skill extraction
[DBG] ChatGPT Response: C#, React, SQL Server, Azure, Docker, ...
[INF] Extracted 8 skills: C#, React, SQL Server, Azure, Docker, CI/CD, Agile, TDD
[DBG] Skill 'C#' extracted for job 1
[DBG] Skill 'React' extracted for job 1
... (for each skill)
[INF] Job 1 processed successfully with 8 skills
```

## Expected Results

After running the worker, you should see:

**JobSkills Table:**
```
JobSkillId | JobRawId | SkillId
-----------|----------|--------
1          | 1        | 3      (e.g., C#)
2          | 1        | 15     (e.g., React)
3          | 1        | 28     (e.g., Azure)
... (100+ total job-skill associations)
```

**Skills Table (Master List):**
```
SkillId | Name
--------|------------------
1       | JavaScript
2       | Python
3       | C#
4       | Java
5       | React
... (50+ total skills extracted)
```

## Testing Scenarios

### Scenario 1: Full End-to-End Test (Recommended)

```bash
# 1. Ensure database is clean
mysql -h 127.0.0.1 -u root -p AiJobMarketIntelligence
DELETE FROM JobSkills;
DELETE FROM JobsProcessed;
DELETE FROM JobsRaw;
DELETE FROM Skills;
EXIT;

# 2. Run the worker
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/

# 3. Let it run for 2-3 minutes (processing all 23 jobs)

# 4. Verify results
mysql -h 127.0.0.1 -u root -p AiJobMarketIntelligence
SELECT COUNT(*) FROM JobSkills;
-- Should show ~150-200 rows
EXIT;
```

### Scenario 2: Test Specific Job

```bash
# 1. Insert a test job
mysql -h 127.0.0.1 -u root -p AiJobMarketIntelligence
INSERT INTO JobsRaw (Title, Company, Description, Location, SalaryRaw, Url, Source)
VALUES (
  'Senior C# Developer',
  'Test Company',
  'We are looking for a Senior C# developer with experience in .NET, Azure, and SQL Server. Must know Docker and Kubernetes.',
  'New York, NY',
  '$120k - $150k',
  'https://example.com/job/1',
  'Test'
);
EXIT;

# 2. Run the worker
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/

# 3. Check extracted skills
mysql -h 127.0.0.1 -u root -p AiJobMarketIntelligence
SELECT s.Name
FROM JobSkills js
JOIN Skills s ON js.SkillId = s.Id
WHERE js.JobRawId = (SELECT MAX(Id) FROM JobsRaw)
ORDER BY s.Name;
-- Should show: Azure, C#, Docker, Kubernetes, SQL Server, .NET, etc.
EXIT;
```

### Scenario 3: Monitor Processing in Real-Time

```bash
# Terminal 1: Run Worker
cd /Users/athulkrishnagopakumar/project/Aijob
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/

# Terminal 2: Watch JobSkills grow
while true; do
  mysql -h 127.0.0.1 -u root -pChangeMe AiJobMarketIntelligence -e \
    "SELECT COUNT(*) as 'Total Job-Skill Associations' FROM JobSkills; \
     SELECT COUNT(DISTINCT JobRawId) as 'Jobs with Skills' FROM JobSkills; \
     SELECT COUNT(*) as 'Unique Skills' FROM Skills;"
  sleep 5
done
```

## Troubleshooting

### Error: "OpenAI API key is required"
```
Solution:
1. Check .env file has OPENAI_API_KEY=sk-...
2. Make sure key starts with "sk-"
3. Restart the worker
```

### Error: "Unauthorized" (API returns 401)
```
Solution:
1. Your API key is invalid or expired
2. Get a new key: https://platform.openai.com/account/api-keys
3. Update .env file
4. Restart the worker
```

### Error: "Rate limit exceeded"
```
Solution:
1. Wait 60 seconds
2. Restart the worker
3. Consider upgrading OpenAI plan
```

### JobSkills table stays empty
```
Solution:
1. Check if jobs are being processed:
   SELECT COUNT(*) FROM JobsProcessed;
   -- Should be > 0

2. Check if skills are being extracted:
   Look at logs for "Extracted X skills" messages
   
3. Check if OpenAI API is working:
   Look for errors in logs

4. Verify skill extraction service is registered:
   grep -r "OpenAiSkillExtractionService" api/src/Worker/
```

## Expected Timeline

| Time | Action |
|------|--------|
| 0:00 | Start worker |
| 0:05 | First job ingestion (23 demo jobs) |
| 0:15 | Job processing starts (1-2 min per job) |
| 1:00 | ~15-20 jobs processed |
| 2:00 | All 23 jobs processed (~150-200 skills) |
| 2:30 | Ready to query and test |

## Sample Queries to Verify Population

### Total Statistics
```sql
SELECT 
  'Total Jobs' as Metric, COUNT(*) as Count FROM JobsRaw
UNION ALL
SELECT 'Total Processed Jobs', COUNT(*) FROM JobsProcessed
UNION ALL
SELECT 'Total Skills Extracted', COUNT(*) FROM JobSkills
UNION ALL
SELECT 'Unique Skills', COUNT(*) FROM Skills;
```

### Top 10 Most Common Skills
```sql
SELECT s.Name, COUNT(*) as Count
FROM JobSkills js
JOIN Skills s ON js.SkillId = s.Id
GROUP BY s.Name
ORDER BY Count DESC
LIMIT 10;
```

### Skills per Job
```sql
SELECT j.Title, COUNT(js.Id) as SkillCount
FROM JobsRaw j
LEFT JOIN JobSkills js ON j.Id = js.JobRawId
GROUP BY j.Id, j.Title
ORDER BY SkillCount DESC;
```

### All Skills for a Specific Job
```sql
SELECT s.Name
FROM JobSkills js
JOIN Skills s ON js.SkillId = s.Id
WHERE js.JobRawId = 1
ORDER BY s.Name;
```

## Performance Notes

- First job: ~3-5 seconds (includes API call)
- Subsequent jobs: ~2-3 seconds each
- Total for 23 jobs: ~2-3 minutes
- Cost: ~$0.002 (23 jobs × $0.0001 per job)

## Next Steps After Population

1. **Verify data:**
   ```bash
   mysql -h 127.0.0.1 -u root -p AiJobMarketIntelligence
   SELECT COUNT(*) FROM JobSkills;
   ```

2. **Query via API:**
   ```bash
   # Run API in another terminal
   dotnet run --project api/src/Api/AiJobMarketIntelligence.Api/
   
   # Then query skills for a job
   curl http://localhost:5062/api/skills/job/1
   ```

3. **Analyze extracted skills:**
   - Most common skills
   - Skills by job type
   - Skills distribution

---

**Ready to populate?** Run the worker command above! 🚀
