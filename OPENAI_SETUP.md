# OpenAI ChatGPT Skill Extraction Setup Guide

## Overview

The skill extraction system has been upgraded to use **OpenAI's ChatGPT API (gpt-4o-mini)** for intelligent, context-aware skill extraction from job descriptions. This replaces the previous regex-based pattern matching with AI-powered analysis.

## What Changed

### Previous Implementation
- Used regex pattern matching against a hardcoded list of ~100 skills
- Limited accuracy and couldn't understand context
- Only matched exact skill names

### New Implementation
- Uses OpenAI ChatGPT (gpt-4o-mini model) for skill extraction
- Context-aware analysis of job descriptions and titles
- Extracts skills mentioned implicitly or explicitly
- Much higher accuracy and coverage
- Automatically handles variations and synonyms

## Setup Instructions

### 1. Get OpenAI API Key

1. Go to [https://platform.openai.com](https://platform.openai.com)
2. Sign up or log in to your account
3. Navigate to **API keys** section: https://platform.openai.com/account/api-keys
4. Click **"Create new secret key"**
5. Copy the key (you won't be able to see it again)

### 2. Configure API Key

The application automatically loads environment variables from a `.env` file. You have three options:

#### Option A: Using .env File (Recommended for local development)

1. Copy the example file:
```bash
cp .env.example .env
```

2. Edit `.env` and add your API key:
```bash
# .env file (do NOT commit this file!)
OPENAI_API_KEY=sk-your-api-key-here
```

3. Run your application (DotNetEnv will automatically load the .env file):
```bash
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

**Important:** The `.env` file is already in `.gitignore` and will NOT be committed to version control.

#### Option B: Environment Variable
```bash
export OPENAI_API_KEY="sk-..."
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

#### Option C: appsettings.json (For development only - NOT recommended)
Edit `appsettings.json` in both API and Worker projects:

**`api/src/Api/AiJobMarketIntelligence.Api/appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Port=3306;Database=AiJobMarketIntelligence;User=root;Password=..."
  },
  "OpenAI": {
    "ApiKey": "sk-your-api-key-here"
  },
  ...
}
```

**`api/src/Worker/AiJobMarketIntelligence.Worker/appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Port=3306;Database=AiJobMarketIntelligence;User=root;Password=..."
  },
  "OpenAI": {
    "ApiKey": "sk-your-api-key-here"
  },
  ...
}
```

**⚠️ WARNING:** Never commit API keys to `appsettings.json`! Use `.env` file instead.

### 3. Verify Configuration

The application will throw an `InvalidOperationException` if the OpenAI API key is not found. This means the configuration lookup order is:

1. `.env` file → `OPENAI_API_KEY` (via DotNetEnv)
2. `OpenAI:ApiKey` from appsettings.json
3. `OPENAI_API_KEY` environment variable

## Running the Application

### Start the Worker (Background Job Processing)
```bash
cd /Users/athulkrishnagopakumar/project/Aijob
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/
```

The worker will:
1. **Ingest jobs** from demo/external APIs (every 30 minutes)
2. **Parse salaries** from raw job data
3. **Extract skills** using ChatGPT (every 15 minutes during processing)
4. **Save extracted skills** to the `JobSkills` table

### Start the API Server
```bash
cd /Users/athulkrishnagopakumar/project/Aijob
dotnet run --project api/src/Api/AiJobMarketIntelligence.Api/
```

Access Swagger documentation at: http://localhost:5062/swagger

## API Integration

The `OpenAiSkillExtractionService` is registered in the DI container and used by:

### In Worker: `JobProcessingWorker`
- Calls `JobProcessingService.ProcessPendingJobsAsync()` periodically
- For each unprocessed job:
  1. Parses salary information
  2. Calls `OpenAiSkillExtractionService.ExtractSkillsAsync()`
  3. Saves extracted skills to database

### In API: `JobsController` (optional)
- Can manually trigger skill extraction via API endpoints
- Reuses the same `ISkillExtractionService` registration

## How Skill Extraction Works

### System Prompt
ChatGPT is given instructions to:
- Extract technical and professional skills
- Look for programming languages, frameworks, databases, cloud platforms, DevOps tools, testing frameworks
- Return results as a comma-separated list
- Filter out irrelevant terms and single characters

### Example Processing
**Job Title:** Senior Full Stack Engineer
**Description:** "Looking for experienced developer with strong C# and React knowledge. Must have 5+ years with SQL Server and Azure cloud..."

**ChatGPT Output:**
```
C#, React, SQL Server, Azure, Full Stack Development, REST APIs, Entity Framework, Git, Agile
```

**Extracted Skills:** 
- C#
- React
- SQL Server
- Azure
- Full Stack Development
- REST APIs
- Entity Framework
- Git
- Agile

## Database Schema

### JobSkills Table
The extracted skills are stored in the `JobSkills` table:

```sql
CREATE TABLE JobSkills (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  JobRawId INT NOT NULL,
  SkillId INT NOT NULL,
  FOREIGN KEY (JobRawId) REFERENCES JobsRaw(Id),
  FOREIGN KEY (SkillId) REFERENCES Skills(Id),
  UNIQUE KEY (JobRawId, SkillId)
);
```

### Skills Table
Master list of all detected skills:

```sql
CREATE TABLE Skills (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  Name VARCHAR(255) NOT NULL UNIQUE
);
```

## Cost Considerations

### Pricing (as of 2026)
- **gpt-4o-mini**: $0.15 per 1M input tokens, $0.60 per 1M output tokens
- Average job description: ~500 tokens
- Average extracted skills: ~100 tokens
- **Cost per job:** ~$0.0001 (very cheap!)

### Optimization Tips
1. **Batch Processing:** Process multiple jobs in batches to reduce API overhead
2. **Caching:** Cache extracted skills for duplicate job descriptions
3. **Fallback:** Keep the regex-based `SkillExtractionService` as fallback if API rate limits are hit

## Troubleshooting

### Error: "OpenAI API key is required"
- Verify `OPENAI_API_KEY` environment variable is set
- OR set `OpenAI:ApiKey` in appsettings.json
- Restart the application

### Error: "Unauthorized"
- API key is invalid or expired
- Generate a new key from https://platform.openai.com/account/api-keys

### Error: "Rate limit exceeded"
- You've hit OpenAI API rate limits
- Wait 60 seconds and retry
- Consider upgrading your OpenAI plan

### Skills Not Being Extracted
- Check application logs for ChatGPT errors
- Verify API key has permissions for chat completions
- Ensure job description is not empty

## Testing

### Unit Tests
All existing unit tests pass with the new implementation:

```bash
cd /Users/athulkrishnagopakumar/project/Aijob
dotnet test tests/UnitTests/UnitTests.csproj
```

Expected output:
```
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

### Manual Testing
1. Start the Worker and API
2. Insert a test job via API:
   ```bash
   POST http://localhost:5062/api/jobs
   ```
3. Wait for the Worker to process (every 15 minutes by default)
4. Query extracted skills:
   ```bash
   GET http://localhost:5062/api/skills/job/{jobId}
   ```

## Switching Back to Regex (If Needed)

If you want to revert to regex-based extraction:

1. Update DI registrations in `Program.cs`:
```csharp
// Comment out OpenAI
// builder.Services.AddSingleton<ISkillExtractionService>(sp =>
//     new OpenAiSkillExtractionService(openAiApiKey, sp.GetRequiredService<ILogger<OpenAiSkillExtractionService>>()));

// Use regex version
builder.Services.AddSingleton<ISkillExtractionService, SkillExtractionService>();
```

2. Remove OpenAI package:
```bash
dotnet remove api/src/Application/AiJobMarketIntelligence.Application/AiJobMarketIntelligence.Application.csproj package OpenAI
```

## Next Steps

- [ ] Set up OpenAI API key
- [ ] Run the Worker
- [ ] Monitor skill extraction in logs
- [ ] Query the `JobSkills` table to verify data
- [ ] (Optional) Implement caching layer for better performance
- [ ] (Optional) Add ML-based filtering to remove false positives

## Files Modified

1. **Created:**
   - `api/src/Application/AiJobMarketIntelligence.Application/Services/Skills/OpenAiSkillExtractionService.cs`

2. **Updated:**
   - `api/src/Application/AiJobMarketIntelligence.Application/AiJobMarketIntelligence.Application.csproj` - Added OpenAI NuGet package
   - `api/src/Api/AiJobMarketIntelligence.Api/Program.cs` - Registered OpenAI service
   - `api/src/Worker/AiJobMarketIntelligence.Worker/Program.cs` - Registered OpenAI service
   - `api/src/Api/AiJobMarketIntelligence.Api/appsettings.json` - Added OpenAI config
   - `api/src/Worker/AiJobMarketIntelligence.Worker/appsettings.json` - Added OpenAI config

## Support

For OpenAI API documentation: https://platform.openai.com/docs/api-reference
For gpt-4o-mini details: https://platform.openai.com/docs/models/gpt-4o-mini
