# Installation & Deployment Guide

## 📋 Prerequisites

- **Operating System**: Windows, macOS, or Linux
- **.NET SDK 10.0**: Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **SQL Server**:
  - Windows: SQL Server 2019+ or SQL Server Express
  - macOS/Linux: Docker or remote SQL Server
- **Git**: For version control
- **IDE**: Visual Studio, Visual Studio Code, or Rider

### Verify Installation

```bash
# Check .NET version
dotnet --version  # Should show 10.x.x

# Verify SQL Server connectivity (Windows with Express)
sqlcmd -S . -E -Q "SELECT @@VERSION;"
```

## 🔧 Local Development Setup

### 1. Clone or Extract Project

```bash
cd /path/to/project
ls -la  # Should show api/, ui/, README.md, etc.
```

### 2. Update Database Connection String

**For Windows (Trusted Connection):**
```json
// appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=AiJobMarketIntelligence;Trusted_Connection=true;TrustServerCertificate=true;"
}
```

**For macOS/Linux (SQL Server Docker):**
```bash
# Start SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123" \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:2019-latest

# Update connection string
"DefaultConnection": "Server=localhost,1433;Database=AiJobMarketIntelligence;User Id=sa;Password=YourPassword123;TrustServerCertificate=true;"
```

**For macOS with M1/M2 Chip:**
- Use SQL Server for macOS in Docker (arm64 compatible)
- Or use Azure SQL Database

### 3. Restore Dependencies

```bash
cd /path/to/project
dotnet restore
```

### 4. Build Solution

```bash
dotnet build
```

Expected output:
```
Build succeeded. 0 Warning(s), 0 Error(s)
```

### 5. Apply Database Migrations

```bash
# From project root
dotnet ef database update \
  --project api/src/Infrastructure/AiJobMarketIntelligence.Infrastructure \
  --startup-project api/src/Api/AiJobMarketIntelligence.Api
```

On first run, this will:
- Create the database: `AiJobMarketIntelligence`
- Create all tables
- Seed skills data

### 6. Run the API

```bash
dotnet run --project api/src/Api/AiJobMarketIntelligence.Api
```

Output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
      Now listening on: http://localhost:5001
```

### 7. Run the Background Worker (Optional)

In a separate terminal:
```bash
dotnet run --project api/src/Worker/AiJobMarketIntelligence.Worker
```

Output:
```
info: AiJobMarketIntelligence.Worker.JobIngestionWorker[0]
      Job Ingestion Worker starting. Interval: 30 minutes
```

## 🧪 Testing the Installation

### 1. Access Swagger UI
Open browser: `https://localhost:7001/swagger/index.html`

### 2. Test API Endpoint
```bash
curl https://localhost:7001/api/jobs \
  -H "Accept: application/json" \
  -k  # Ignore SSL for localhost
```

### 3. Manual Job Ingestion
```bash
curl -X POST https://localhost:7001/api/admin/trigger-fetch \
  -H "Content-Type: application/json" \
  -k
```

### 4. Search Jobs
```bash
curl "https://localhost:7001/api/jobs/search?keyword=backend" -k
```

## 🐳 Docker Deployment

### Create Dockerfile for API

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution
COPY . .

# Restore and build
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80 443
ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENTRYPOINT ["dotnet", "AiJobMarketIntelligence.Api.dll"]
```

### Create Dockerfile for Worker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish -p:PublishProfile=DefaultContainer -p:PublishDir=/app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AiJobMarketIntelligence.Worker.dll"]
```

### Create docker-compose.yml

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "YourPassword123"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $$SA_PASSWORD -Q "SELECT 1"
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "7001:443"
      - "5001:80"
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;Database=AiJobMarketIntelligence;User Id=sa;Password=YourPassword123;TrustServerCertificate=true;"
    depends_on:
      sqlserver:
        condition: service_healthy
    restart: unless-stopped

  worker:
    build:
      context: .
      dockerfile: Dockerfile.Worker
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;Database=AiJobMarketIntelligence;User Id=sa;Password=YourPassword123;TrustServerCertificate=true;"
    depends_on:
      sqlserver:
        condition: service_healthy
    restart: unless-stopped

volumes:
  sqlserver_data:
```

### Deploy with Docker Compose

```bash
# Build images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop services
docker-compose down
```

## ☁️ Azure Deployment

### 1. Create Azure Resources

```bash
# Create resource group
az group create --name aijobgroup --location eastus

# Create App Service plan
az appservice plan create --name aijobjob --resource-group aiJobgroup --sku B2

# Create Web App
az webapp create --resource-group aiJobgroup \
  --plan aijobjob \
  --name aijobapi \
  --runtime "DOTNET|10.0"

# Create SQL Database
az sql server create --name aiJobserver \
  --resource-group aiJobgroup \
  --admin-user sqladmin \
  --admin-password "YourPassword123!"

az sql db create --resource-group aiJobgroup \
  --server aiJobserver \
  --name AiJobMarketIntelligence
```

### 2. Configure Connection String

```bash
az webapp config connection-string set \
  --resource-group aiJobgroup \
  --name aijobapi \
  --settings \
  DefaultConnection='Server=tcp:aiJobserver.database.windows.net,1433;Initial Catalog=AiJobMarketIntelligence;Persist Security Info=False;User ID=sqladmin;Password=YourPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;' \
  --connection-string-type SQLServer
```

### 3. Publish to Azure

```bash
# Build release package
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy using ZIP
az webapp deployment source config-zip \
  --resource-group aiJobgroup \
  --name aijobapi \
  --src deploy.zip
```

### 4. Deploy Worker as Azure Function (Optional)

Convert Worker to Azure Functions Timer Trigger pattern for serverless deployment.

## 🔐 Production Configuration

### Environment Variables Setup

Create `.env` file (never commit to Git):
```bash
# Database
DB_SERVER=your-sql-server.database.windows.net
DB_NAME=AiJobMarketIntelligence
DB_USER=sqladmin
DB_PASSWORD=YourSecurePassword123!

# API Keys (if using real providers)
ADZUNA_APP_ID=your-app-id
ADZUNA_APP_KEY=your-app-key

# Job Ingestion
JOB_INGESTION_INTERVAL=30
JOB_PROVIDER=MockAPI  # Or: Adzuna, LinkedIn, etc.

# Logging
LOG_LEVEL=Information
```

### Update Program.cs for Production

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load environment variables
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Connection string from environment
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AiJobContext>(options =>
    options.UseSqlServer(connectionString));
```

### Security Best Practices

1. **API Authentication**
   ```csharp
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options => { /* JWT config */ });
   ```

2. **CORS Configuration**
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("Production", policy =>
       {
           policy.WithOrigins("https://yourdomain.com")
               .AllowAnyMethod()
               .AllowAnyHeader();
       });
   });
   ```

3. **HTTPS Enforcement**
   ```csharp
   app.UseHttpsRedirection();
   ```

4. **Rate Limiting**
   ```csharp
   builder.Services.AddRateLimiter(options =>
   {
       options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
           RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
               factory: partition => new FixedWindowRateLimiterOptions
               {
                   PermitLimit = 100,
                   Window = TimeSpan.FromMinutes(1)
               }));
   });
   ```

## 🔄 Database Migrations in Production

### Create Migration Script

```bash
dotnet ef migrations script --project api/src/Infrastructure \
  --startup-project api/src/Api \
  --output migration.sql
```

### Review and Execute Safely

1. Review `migration.sql` for any dangerous operations
2. Create database backup
3. Execute in staging first
4. Then execute in production

## 📊 Monitoring & Logging

### Application Insights Integration

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Structured Logging

```csharp
// Use Serilog for production-grade logging
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File("logs/api-.txt", rollingInterval: RollingInterval.Day));
```

## ✅ Post-Deployment Checklist

- [ ] Database connection verified
- [ ] Migrations applied successfully
- [ ] API endpoints responding
- [ ] Swagger UI accessible
- [ ] Worker service running (if deployed)
- [ ] Logs configured and working
- [ ] SSL/HTTPS configured
- [ ] Firewall rules updated
- [ ] Backups configured
- [ ] Monitoring alerts set up

## 🆘 Troubleshooting

### Common Issues

**Connection Timeout**
```bash
# Verify SQL Server is running
sudo systemctl status mssql-server  # Linux
sqlcmd -S . -E -Q "SELECT 1"        # Windows
```

**Migration Fails**
```bash
# Check migrations exist
dotnet ef migrations list --project api/src/Infrastructure

# Remove bad migration
dotnet ef migrations remove --project api/src/Infrastructure

# Recreate
dotnet ef migrations add FixedMigration --project api/src/Infrastructure
```

**Port Already in Use**
```bash
# Find process using port 5001
lsof -i :5001  # macOS/Linux
netstat -ano | findstr :5001  # Windows

# Kill process
kill -9 <PID>  # macOS/Linux
taskkill /PID <PID> /F  # Windows
```

## 📞 Support

For issues:
1. Check the logs: `dotnet run --project api/src/Api`
2. Review [Development Guide](./DEVELOPMENT.md)
3. Check [README.md](./README.md)
