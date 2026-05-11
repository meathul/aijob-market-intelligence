# .env File Setup Guide

## Overview

This project uses a `.env` file to securely store sensitive configuration like API keys. The `.env` file is **automatically loaded** when you run the application and **never committed to git**.

## What is a .env File?

A `.env` (environment) file is a simple text file that stores environment variables used by your application. It's automatically loaded by the `DotNetEnv` NuGet package.

**Example `.env` file:**
```
OPENAI_API_KEY=sk-proj-abc123xyz...
DB_PASSWORD=mySecurePassword123
```

## Setup Instructions

### Step 1: Copy the Example File

```bash
cd /Users/athulkrishnagopakumar/project/Aijob
cp .env.example .env
```

This creates a new `.env` file from the template.

### Step 2: Add Your API Key

1. Get your OpenAI API key:
   - Visit: https://platform.openai.com/account/api-keys
   - Click: "Create new secret key"
   - Copy the key (format: `sk-...`)

2. Edit the `.env` file:
   ```bash
   # Using nano
   nano .env
   
   # Using vim
   vim .env
   
   # Or open with your editor
   ```

3. Add your API key:
   ```
   OPENAI_API_KEY=sk-your-key-here
   ```

### Step 3: Verify It Works

The application will:
1. Automatically read the `.env` file
2. Load `OPENAI_API_KEY` into the environment
3. Use it for ChatGPT skill extraction

Test it:
```bash
# Run the Worker
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker/

# You should see: "Calling OpenAI ChatGPT for skill extraction"
```

## File Structure

```
project-root/
├── .env                    ← Local secrets (NOT in git)
├── .env.example            ← Template (IN git)
├── .gitignore              ← Already excludes .env
└── ... other files
```

## Security Best Practices

✅ **DO:**
- Keep `.env` in `.gitignore` (already configured)
- Use different keys for different environments
- Rotate keys regularly
- Never share your `.env` file
- Use `.env.example` as template for team members

❌ **DON'T:**
- Commit `.env` to git
- Share keys via email or chat
- Store keys in code comments
- Use the same key for multiple environments
- Leave dummy keys in `.env`

## What Goes in .env?

### Required:
```
OPENAI_API_KEY=sk-your-api-key-here
```

### Optional:
```
# Database (if not using appsettings.json)
DB_CONNECTION_STRING=Server=127.0.0.1;Port=3306;Database=AiJobMarketIntelligence;User=root;Password=your_password

# Logging level
LOG_LEVEL=Information

# API port
API_PORT=5062
```

## Configuration Priority

The application looks for `OPENAI_API_KEY` in this order:

1. **`.env` file** (loaded first by DotNetEnv)
2. **Environment variable** (Windows: set OPENAI_API_KEY=..., Linux/Mac: export OPENAI_API_KEY=...)
3. **appsettings.json** (hardcoded, not recommended)

## For Team Members

### Sharing Setup with Your Team

1. Commit `.env.example` to git (already done):
   ```bash
   git add .env.example
   git commit -m "Add .env template"
   ```

2. Team members clone the repo and copy the template:
   ```bash
   git clone <your-repo>
   cp .env.example .env
   # Then add their own API key
   ```

3. `.env` is automatically ignored by git (already in `.gitignore`)

## Production Deployment

For production, instead of using a `.env` file:

1. Set environment variables on your server:
   ```bash
   export OPENAI_API_KEY="sk-prod-key-here"
   ```

2. Or use Docker secrets:
   ```dockerfile
   ENV OPENAI_API_KEY=${OPENAI_API_KEY}
   ```

3. Or use cloud provider secrets management:
   - AWS Secrets Manager
   - Azure Key Vault
   - Kubernetes Secrets
   - etc.

## Troubleshooting

### Error: "OpenAI API key is required"

**Cause:** The application couldn't find your API key

**Solution:**
1. Verify `.env` file exists:
   ```bash
   ls -la .env
   ```

2. Verify it has the correct content:
   ```bash
   cat .env
   # Should show: OPENAI_API_KEY=sk-...
   ```

3. Restart the application (files are loaded on startup)

### Error: "Unauthorized" when calling OpenAI API

**Cause:** API key is invalid or expired

**Solution:**
1. Verify your API key is correct:
   ```bash
   cat .env | grep OPENAI_API_KEY
   ```

2. Get a new key from https://platform.openai.com/account/api-keys

3. Update your `.env` file with the new key

4. Restart the application

### .env file is being committed to git

**Cause:** `.gitignore` might be incorrect

**Solution:**
1. Check `.gitignore`:
   ```bash
   grep "\.env" .gitignore
   ```

2. If missing, add it:
   ```bash
   echo ".env" >> .gitignore
   ```

3. Remove accidentally committed file:
   ```bash
   git rm --cached .env
   git commit -m "Remove .env from tracking"
   ```

## Example .env Files

### Development
```
OPENAI_API_KEY=sk-proj-dev-abc123...
LOG_LEVEL=Debug
```

### Testing
```
OPENAI_API_KEY=sk-proj-test-xyz789...
LOG_LEVEL=Information
```

### Production (not recommended as .env)
Use environment variables or cloud secrets instead:
```bash
export OPENAI_API_KEY="sk-proj-prod-abc123..."
```

## Additional Resources

- DotNetEnv GitHub: https://github.com/toannguyen/DotNetEnv
- OpenAI API Keys: https://platform.openai.com/account/api-keys
- Environment Variables Best Practices: https://12factor.net/config

## Summary

1. Copy `.env.example` to `.env`
2. Add your API key to `.env`
3. Run the application (it loads automatically)
4. Never commit `.env` to git
5. Share `.env.example` with team, not `.env`

---

**That's it!** Your secrets are now secure and won't be accidentally committed to version control. 🔐
