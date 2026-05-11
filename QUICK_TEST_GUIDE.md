# Quick Test Commands

## 🚀 Start the Worker (Populate JobSkills)

```bash
cd /Users/athulkrishnagopakumar/project/Aijob

# Option 1: Using environment variable
export OPENAI_API_KEY="sk-your-api-key-here"
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/

# Option 2: Using .env file (automatic)
# Just edit .env with your API key and run:
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

## 🔍 Monitor Database

Open a new terminal:

```bash
# Connect to MySQL
mysql -u root -p AiJobMarketIntelligence

# Check job counts
SELECT 'JobsRaw' as Table, COUNT(*) as Count FROM JobsRaw
UNION ALL
SELECT 'JobsProcessed', COUNT(*) FROM JobsProcessed
UNION ALL
SELECT 'JobSkills', COUNT(*) FROM JobSkills
UNION ALL
SELECT 'Skills', COUNT(*) FROM Skills;

# See extracted skills for first job
SELECT jr.Title, GROUP_CONCAT(s.Name SEPARATOR ', ') as Skills
FROM JobSkills js
JOIN JobsRaw jr ON js.JobRawId = jr.Id
JOIN Skills s ON js.SkillId = s.Id
WHERE jr.Id = 1
GROUP BY jr.Id, jr.Title;

# Most common skills
SELECT s.Name, COUNT(*) as Count
FROM JobSkills js
JOIN Skills s ON js.SkillId = s.Id
GROUP BY s.Name
ORDER BY Count DESC
LIMIT 10;
```

## 📊 Run API to Query Results

```bash
# In a third terminal
cd /Users/athulkrishnagopakumar/project/Aijob
export OPENAI_API_KEY="sk-your-api-key-here"
dotnet run --project api/src/Api/AiJobMarketIntelligence.Api/

# Then visit: http://localhost:5062/swagger
# Test endpoints like:
# GET /api/skills/search?term=react
# GET /api/jobs
```

## ✅ Run Unit Tests

```bash
cd /Users/athulkrishnagopakumar/project/Aijob
dotnet test tests/UnitTests/UnitTests.csproj

# Expected output:
# Passed!  - Failed: 0, Passed: 6, Total: 6
```

## 🔧 Verify Setup

```bash
# Check .env file
cat .env

# Should show: OPENAI_API_KEY=sk-...
```

## ⏱️ Timing

- **Ingestion**: Happens every 30 minutes
- **Processing + Skill Extraction**: Happens every 15 minutes
- **Expected first results**: 15 minutes after Worker start

To see immediate results in testing:
1. Check logs for "Extracted X skills"
2. Query database every 15 seconds
3. Skills should appear within first 15 minutes

## 📝 What to Expect

After running for 15 minutes, you should see:

```
JobsRaw table:       23+ jobs
JobsProcessed table: 23+ jobs (with salary parsed)
JobSkills table:     150-200+ skill associations
Skills table:        100-150+ unique skills
```

Sample extracted skills:
- C#, React, Azure, Docker, SQL Server, Python, Django, PostgreSQL, etc.

---

Full guide: See `TESTING_GUIDE.md`
