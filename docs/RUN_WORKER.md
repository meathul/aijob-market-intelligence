# Run Worker to Populate JobSkills Table

## ⚡ Quick Command

```bash
cd /Users/athulkrishnagopakumar/project/Aijob
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

## Step-by-Step Instructions

### 1. Update .env with Your API Key
```bash
# Edit the .env file
nano .env

# Add your real OpenAI API key (get from https://platform.openai.com/account/api-keys)
OPENAI_API_KEY=sk-proj-your-real-key-here
```

### 2. Navigate to Project Directory
```bash
cd /Users/athulkrishnagopakumar/project/Aijob
```

### 3. Run the Worker
```bash
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

### 4. Watch the Output
You should see:
```
[INF] JobIngestionWorker: Starting job ingestion...
[INF] 23 jobs ingested successfully from demo provider
[INF] JobProcessingWorker: Processing pending jobs...
[INF] Processing job 1: "Senior Developer"
[INF] Calling OpenAI ChatGPT for skill extraction
[INF] Extracted 12 skills: C#, .NET, React, Azure, ...
[INF] Job 1 processed successfully
... (continues for all 23 jobs)
[INF] All jobs processed! 23 total processed.
```

### 5. Verify JobSkills Table Population

**While the worker is running (in another terminal):**
```bash
# Connect to MySQL
mysql -h 127.0.0.1 -u root -p AiJobMarketIntelligence

# Check progress
SELECT COUNT(*) FROM JobSkills;

# Exit
EXIT;
```

Expected results:
- After 1 minute: ~20-40 skills
- After 2 minutes: ~100-150 skills  
- After 3 minutes: ~150-200 skills (complete)

## What the Worker Does

1. **Ingest Jobs** (every 30 min)
   - Fetches 23 demo jobs
   - Stores in `JobsRaw` table

2. **Process Jobs** (every 15 min)
   - Reads unprocessed jobs
   - Parses salary information
   - **Extracts skills using ChatGPT** ← NEW!
   - Saves to `JobsProcessed` table
   - **Populates `JobSkills` table** ← This is what we're testing

3. **Save Skills**
   - Stores each skill in `Skills` table
   - Creates job-to-skill mappings in `JobSkills` table

## Timeline

| Time | What Happens |
|------|--------------|
| 0:00 | Worker starts |
| 0:05 | Jobs ingested (if first run) |
| 0:15 | Processing starts |
| 1:30 | ~15 jobs processed |
| 2:30 | All 23 jobs done |
| 3:00 | Ready to query results |

## Verify Results

### Query 1: Count total skills extracted
```sql
SELECT COUNT(*) as 'Total Job-Skill Associations' FROM JobSkills;
```

Expected: 150-200+

### Query 2: Top 10 skills
```sql
SELECT s.Name, COUNT(*) as Count
FROM JobSkills js
JOIN Skills s ON js.SkillId = s.Id
GROUP BY s.Name
ORDER BY Count DESC
LIMIT 10;
```

Expected output:
```
Name            Count
C#              15
Python          12
React           10
Azure            8
Docker           7
...
```

### Query 3: Skills per job
```sql
SELECT j.Title, COUNT(js.Id) as SkillCount
FROM JobsRaw j
LEFT JOIN JobSkills js ON j.Id = js.JobRawId
GROUP BY j.Id, j.Title
ORDER BY SkillCount DESC;
```

## Stop the Worker

Press `Ctrl+C` in the terminal where the worker is running.

## Troubleshooting

### Error: "OpenAI API key is required"
- Check `.env` file has your real API key
- Make sure key starts with `sk-proj-`
- Restart the worker

### Error: "Unauthorized"  
- Your API key is invalid
- Get a new one: https://platform.openai.com/account/api-keys
- Update `.env`
- Restart worker

### JobSkills table not growing
- Check logs for "Extracted X skills" messages
- Verify MySQL connection is working
- Check database credentials in appsettings.json

## Database Schema

### Jobs Flow
```
JobsRaw (23 jobs ingested)
    ↓
JobProcessingWorker processes
    ├─ Parse salary
    ├─ Extract skills
    └─ Save results
    ↓
JobsProcessed (23 processed records)
+ JobSkills (150-200+ job-skill associations)
+ Skills (50+ unique skills)
```

---

**Ready?** Just run the command and watch the magic happen! 🚀
