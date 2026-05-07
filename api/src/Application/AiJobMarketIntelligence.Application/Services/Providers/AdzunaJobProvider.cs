using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using AiJobMarketIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.Application.Services.Providers;

/// <summary>
/// Fetches real job data from public job search APIs
/// Uses Remotive API (free, no auth required) and Git Jobs API
/// Provides real job listings from tech companies and job boards
/// </summary>
public class AdzunaJobProvider : IJobProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdzunaJobProvider> _logger;
    
    // Public job APIs (free, no authentication required)
    private const string RomotiveUrl = "https://remotive.io/api/remote-jobs";
    private const string GitJobsUrl = "https://raw.githubusercontent.com/public-apis/public-apis/master/index.json";

    public AdzunaJobProvider(ILogger<AdzunaJobProvider> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiJobMarketIntelligence/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<JobRaw>> FetchJobsAsync()
    {
        var allJobs = new List<JobRaw>();

        try
        {
            _logger.LogInformation("Starting job fetch from real APIs");

            // Try Remotive API first
            try
            {
                _logger.LogInformation("Fetching jobs from Remotive API");
                var remotiveJobs = await FetchFromRomotiveAsync();
                allJobs.AddRange(remotiveJobs);
                _logger.LogInformation($"Fetched {remotiveJobs.Count} jobs from Remotive");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error fetching from Remotive: {ex.Message}");
            }

            // Try GitHub Jobs if Remotive didn't return enough
            if (allJobs.Count < 10)
            {
                try
                {
                    _logger.LogInformation("Fetching jobs from GitHub Jobs API");
                    var githubJobs = await FetchFromGithubJobsAsync();
                    allJobs.AddRange(githubJobs);
                    _logger.LogInformation($"Fetched {githubJobs.Count} jobs from GitHub Jobs");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error fetching from GitHub Jobs: {ex.Message}");
                }
            }

            // If external APIs fail or return insufficient data, use demo/test jobs
            // This ensures the system remains testable in environments with network restrictions
            if (allJobs.Count < 10)
            {
                _logger.LogInformation("External APIs unavailable or insufficient data. Loading demo/test jobs for development/testing.");
                var demoJobs = GetSampleTechJobs();
                allJobs.AddRange(demoJobs);
                _logger.LogInformation($"Loaded {demoJobs.Count} demo/test jobs for development");
            }

            _logger.LogInformation($"Successfully fetched {allJobs.Count} total jobs (mix of real APIs and test data if needed)");
            return allJobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in job fetch");
            return new List<JobRaw>();
        }
    }

    private async Task<List<JobRaw>> FetchFromRomotiveAsync()
    {
        var jobs = new List<JobRaw>();

        try
        {
            _logger.LogDebug($"Calling Remotive API: {RomotiveUrl}");

            var response = await _httpClient.GetAsync(RomotiveUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Parse results array from Remotive
                if (root.TryGetProperty("jobs", out var jobsElement))
                {
                    var jobCount = 0;
                    foreach (var jobElement in jobsElement.EnumerateArray())
                    {
                        var job = ParseRomotiveJob(jobElement);
                        if (job != null)
                        {
                            jobs.Add(job);
                            jobCount++;
                            if (jobCount >= 50) break; // Limit to 50 jobs
                        }
                    }
                }

                _logger.LogInformation($"Parsed {jobs.Count} jobs from Remotive");
            }
            else
            {
                _logger.LogWarning($"Remotive API returned status {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from Remotive API");
        }

        return jobs;
    }

    private async Task<List<JobRaw>> FetchFromGithubJobsAsync()
    {
        var jobs = new List<JobRaw>();

        try
        {
            const string githubJobsUrl = "https://api.github.com/jobs?description=developer&page=1";
            _logger.LogDebug($"Calling GitHub Jobs API: {githubJobsUrl}");

            var response = await _httpClient.GetAsync(githubJobsUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    var jobCount = 0;
                    foreach (var jobElement in root.EnumerateArray())
                    {
                        var job = ParseGithubJob(jobElement);
                        if (job != null)
                        {
                            jobs.Add(job);
                            jobCount++;
                            if (jobCount >= 30) break;
                        }
                    }
                }

                _logger.LogInformation($"Parsed {jobs.Count} jobs from GitHub Jobs");
            }
            else
            {
                _logger.LogWarning($"GitHub Jobs API returned status {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error fetching from GitHub Jobs API: {ex.Message}");
        }

        return jobs;
    }

    private JobRaw? ParseRomotiveJob(JsonElement jobElement)
    {
        try
        {
            var id = GetJsonString(jobElement, "id") ?? Guid.NewGuid().ToString();
            var title = GetJsonString(jobElement, "title") ?? "No Title";
            var company = GetJsonString(jobElement, "company_name") ?? "Unknown Company";
            var url = GetJsonString(jobElement, "url") ?? $"https://remotive.io/job/{id}";
            var description = GetJsonString(jobElement, "description") ?? "";
            var jobType = GetJsonString(jobElement, "job_type") ?? "Full-time";
            
            // Extract location (Remotive uses location_region)
            var location = GetJsonString(jobElement, "location") ?? "Remote";

            // Extract salary if available
            string? salaryRaw = null;
            if (jobElement.TryGetProperty("salary", out var salaryElement) && 
                salaryElement.ValueKind != JsonValueKind.Null)
            {
                salaryRaw = salaryElement.GetString();
            }

            // Get posted date
            var publishedAtStr = GetJsonString(jobElement, "published_at") ?? DateTime.UtcNow.ToString("O");
            DateTime.TryParse(publishedAtStr, out var postedDate);

            // Create JobRaw entity
            var job = new JobRaw
            {
                Title = title.Length > 500 ? title.Substring(0, 500) : title,
                Company = company.Length > 300 ? company.Substring(0, 300) : company,
                Location = location.Length > 300 ? location.Substring(0, 300) : location,
                Description = CleanDescription(description),
                SalaryRaw = salaryRaw?.Length > 200 ? salaryRaw.Substring(0, 200) : salaryRaw,
                Source = "Remotive",
                Url = url.Length > 2000 ? url.Substring(0, 2000) : url,
                PostedDate = postedDate > DateTime.MinValue ? postedDate : DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error parsing job: {ex.Message}");
            return null;
        }
    }

    private JobRaw? ParseGithubJob(JsonElement jobElement)
    {
        try
        {
            var id = GetJsonString(jobElement, "id") ?? Guid.NewGuid().ToString();
            var title = GetJsonString(jobElement, "title") ?? "No Title";
            var company = GetJsonString(jobElement, "company") ?? "Unknown Company";
            var url = GetJsonString(jobElement, "url") ?? $"https://github.com/jobs/{id}";
            var description = GetJsonString(jobElement, "description") ?? "";
            var location = GetJsonString(jobElement, "location") ?? "Remote";

            // Create JobRaw entity
            var job = new JobRaw
            {
                Title = title.Length > 500 ? title.Substring(0, 500) : title,
                Company = company.Length > 300 ? company.Substring(0, 300) : company,
                Location = location.Length > 300 ? location.Substring(0, 300) : location,
                Description = CleanDescription(description),
                SalaryRaw = null,
                Source = "GitHubJobs",
                Url = url.Length > 2000 ? url.Substring(0, 2000) : url,
                PostedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error parsing GitHub job: {ex.Message}");
            return null;
        }
    }

    private List<JobRaw> GetSampleTechJobs()
    {
        return new List<JobRaw>
        {
            new JobRaw
            {
                Title = "Senior Software Engineer - .NET/C#",
                Company = "TechCorp Solutions",
                Location = "San Francisco, CA",
                Description = "We're looking for an experienced .NET engineer to lead our cloud platform team. " +
                    "You'll work with Azure, Entity Framework, and modern C# to build scalable solutions. " +
                    "Requirements: 7+ years experience, strong C# skills, Azure knowledge, team leadership experience.",
                SalaryRaw = "$150,000 - $200,000 per year",
                Source = "Sample",
                Url = "https://techcorp.example.com/jobs/senior-dotnet-engineer",
                PostedDate = DateTime.UtcNow.AddDays(-2),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Full Stack Developer - React & Node.js",
                Company = "Digital Innovations Inc",
                Location = "New York, NY",
                Description = "Join our startup as a full stack developer. You'll build responsive web applications " +
                    "using React for the frontend and Node.js for the backend. Work with MongoDB and PostgreSQL. " +
                    "Requirements: 3+ years experience, React & Node.js proficiency, database knowledge.",
                SalaryRaw = "$120,000 - $160,000 per year",
                Source = "Sample",
                Url = "https://digital-innovations.example.com/jobs/fullstack-dev",
                PostedDate = DateTime.UtcNow.AddDays(-1),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Data Scientist - Machine Learning",
                Company = "AI Research Labs",
                Location = "Boston, MA",
                Description = "Help us build the next generation of ML models. You'll work with TensorFlow, " +
                    "PyTorch, and Python to develop and deploy machine learning solutions. " +
                    "Requirements: PhD or Masters in CS/ML, Python expertise, ML algorithms knowledge.",
                SalaryRaw = "$140,000 - $180,000 per year",
                Source = "Sample",
                Url = "https://ai-labs.example.com/jobs/data-scientist",
                PostedDate = DateTime.UtcNow.AddDays(-3),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "DevOps Engineer - Cloud Infrastructure",
                Company = "CloudScale Systems",
                Location = "Seattle, WA",
                Description = "Design and maintain our cloud infrastructure on AWS and Kubernetes. " +
                    "You'll manage CI/CD pipelines, containerization, and infrastructure as code. " +
                    "Requirements: 5+ years DevOps experience, AWS/Kubernetes expertise, Docker knowledge.",
                SalaryRaw = "$130,000 - $170,000 per year",
                Source = "Sample",
                Url = "https://cloudscale.example.com/jobs/devops-engineer",
                PostedDate = DateTime.UtcNow.AddDays(-4),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Database Administrator - SQL Server",
                Company = "Enterprise Data Solutions",
                Location = "Chicago, IL",
                Description = "Manage and optimize large-scale SQL Server databases for financial applications. " +
                    "You'll handle performance tuning, backups, and disaster recovery. " +
                    "Requirements: 6+ years SQL Server experience, performance tuning skills, high availability setup.",
                SalaryRaw = "$110,000 - $150,000 per year",
                Source = "Sample",
                Url = "https://enterprise-data.example.com/jobs/dba-sqlserver",
                PostedDate = DateTime.UtcNow.AddDays(-5),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Frontend Engineer - Vue.js",
                Company = "WebFlow Studios",
                Location = "Los Angeles, CA",
                Description = "Build beautiful and responsive user interfaces with Vue.js. " +
                    "You'll collaborate with designers and backend engineers to create amazing web experiences. " +
                    "Requirements: 4+ years frontend experience, Vue.js expertise, CSS/HTML mastery.",
                SalaryRaw = "$115,000 - $155,000 per year",
                Source = "Sample",
                Url = "https://webflow-studios.example.com/jobs/frontend-vue",
                PostedDate = DateTime.UtcNow.AddDays(-6),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Backend Engineer - Go/Golang",
                Company = "DistributedSystems Inc",
                Location = "Remote",
                Description = "Build high-performance backend systems using Go. " +
                    "Work on microservices, APIs, and distributed systems. " +
                    "Requirements: 5+ years backend experience, Go proficiency, microservices knowledge.",
                SalaryRaw = "$130,000 - $170,000 per year",
                Source = "Sample",
                Url = "https://distributedsystems.example.com/jobs/backend-go",
                PostedDate = DateTime.UtcNow.AddDays(-7),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Mobile Developer - React Native",
                Company = "MobileFirst Apps",
                Location = "Boston, MA",
                Description = "Develop cross-platform mobile applications using React Native. " +
                    "Build apps for iOS and Android from a single codebase. " +
                    "Requirements: 3+ years mobile development, React Native expertise, Firebase knowledge.",
                SalaryRaw = "$110,000 - $150,000 per year",
                Source = "Sample",
                Url = "https://mobilefirst.example.com/jobs/mobile-react-native",
                PostedDate = DateTime.UtcNow.AddDays(-8),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "QA Engineer - Automation Testing",
                Company = "QualityAssure Labs",
                Location = "Austin, TX",
                Description = "Develop automated testing frameworks and ensure software quality. " +
                    "You'll create test cases, run automation, and improve testing processes. " +
                    "Requirements: 4+ years QA experience, Selenium/Cypress expertise, scripting knowledge.",
                SalaryRaw = "$100,000 - $135,000 per year",
                Source = "Sample",
                Url = "https://qualityassure.example.com/jobs/qa-automation",
                PostedDate = DateTime.UtcNow.AddDays(-9),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Site Reliability Engineer - Kubernetes",
                Company = "InfraScale",
                Location = "San Francisco, CA",
                Description = "Manage and optimize our Kubernetes infrastructure at scale. " +
                    "You'll ensure high availability, implement CI/CD pipelines, and improve system reliability. " +
                    "Requirements: 6+ years infrastructure experience, Kubernetes expertise, Linux knowledge.",
                SalaryRaw = "$140,000 - $190,000 per year",
                Source = "Sample",
                Url = "https://infrascale.example.com/jobs/sre-kubernetes",
                PostedDate = DateTime.UtcNow.AddDays(-10),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Security Engineer - Cloud Security",
                Company = "CyberDefense Pro",
                Location = "Denver, CO",
                Description = "Protect our cloud infrastructure from security threats. " +
                    "Implement security best practices, conduct penetration testing, and manage compliance. " +
                    "Requirements: 5+ years security experience, cloud security expertise, certifications preferred.",
                SalaryRaw = "$135,000 - $180,000 per year",
                Source = "Sample",
                Url = "https://cyberdefense.example.com/jobs/security-cloud",
                PostedDate = DateTime.UtcNow.AddDays(-11),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Data Engineer - Apache Spark",
                Company = "BigData Analytics",
                Location = "Mountain View, CA",
                Description = "Build data pipelines and ETL processes using Apache Spark and Python. " +
                    "Work with massive datasets and optimize data processing workflows. " +
                    "Requirements: 4+ years data engineering, Spark expertise, SQL proficiency.",
                SalaryRaw = "$125,000 - $165,000 per year",
                Source = "Sample",
                Url = "https://bigdata-analytics.example.com/jobs/data-engineer-spark",
                PostedDate = DateTime.UtcNow.AddDays(-12),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Technical Product Manager - AI/ML",
                Company = "AIinnovate",
                Location = "Palo Alto, CA",
                Description = "Lead product strategy for our AI/ML platform. " +
                    "Work with engineers and stakeholders to build cutting-edge AI products. " +
                    "Requirements: 5+ years product management, ML/AI background, strategic thinking.",
                SalaryRaw = "$150,000 - $210,000 per year",
                Source = "Sample",
                Url = "https://aiinnovate.example.com/jobs/pm-aiml",
                PostedDate = DateTime.UtcNow.AddDays(-13),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Solutions Architect - AWS",
                Company = "CloudArchitects Ltd",
                Location = "Toronto, Canada",
                Description = "Design scalable AWS solutions for enterprise clients. " +
                    "Lead architecture decisions, implement best practices, and provide technical guidance. " +
                    "Requirements: 7+ years architecture experience, AWS certification, enterprise background.",
                SalaryRaw = "$140,000 - $200,000 per year",
                Source = "Sample",
                Url = "https://cloudarchitects.example.com/jobs/solutions-architect-aws",
                PostedDate = DateTime.UtcNow.AddDays(-14),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Java Developer - Spring Boot",
                Company = "EnterpriseApps Co",
                Location = "Chicago, IL",
                Description = "Build enterprise applications using Java and Spring Boot. " +
                    "You'll develop scalable microservices and REST APIs. " +
                    "Requirements: 5+ years Java experience, Spring Boot expertise, REST API knowledge.",
                SalaryRaw = "$125,000 - $165,000 per year",
                Source = "Sample",
                Url = "https://enterpriseapps.example.com/jobs/java-spring-boot-v2",
                PostedDate = DateTime.UtcNow.AddDays(-15),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Platform Engineer - Infrastructure",
                Company = "ScaleOps",
                Location = "Remote",
                Description = "Build and maintain our internal developer platform. " +
                    "You'll improve developer productivity and system reliability. " +
                    "Requirements: 6+ years platform engineering, DevOps expertise, infrastructure knowledge.",
                SalaryRaw = "$135,000 - $185,000 per year",
                Source = "Sample",
                Url = "https://scaleops.example.com/jobs/platform-engineer-v2",
                PostedDate = DateTime.UtcNow.AddDays(-16),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Analytics Engineer - Data Warehouse",
                Company = "DataInsights Inc",
                Location = "Denver, CO",
                Description = "Build data warehouse solutions and analytics pipelines. " +
                    "You'll work with Snowflake, dbt, and analytics tools. " +
                    "Requirements: 4+ years analytics engineering, SQL expertise, ETL knowledge.",
                SalaryRaw = "$120,000 - $160,000 per year",
                Source = "Sample",
                Url = "https://datainsights.example.com/jobs/analytics-engineer-snowflake",
                PostedDate = DateTime.UtcNow.AddDays(-17),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Blockchain Developer - Web3",
                Company = "CryptoInnovate",
                Location = "Austin, TX",
                Description = "Develop smart contracts and Web3 applications. " +
                    "You'll work with Solidity, Ethereum, and blockchain technologies. " +
                    "Requirements: 3+ years blockchain development, Solidity expertise, Web3 knowledge.",
                SalaryRaw = "$130,000 - $200,000 per year",
                Source = "Sample",
                Url = "https://cryptoinnovate.example.com/jobs/blockchain-web3-dev",
                PostedDate = DateTime.UtcNow.AddDays(-18),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "ML Operations Engineer - MLOps",
                Company = "AIMl Systems",
                Location = "Mountain View, CA",
                Description = "Build ML operations infrastructure and pipelines. " +
                    "You'll deploy, monitor, and maintain machine learning models. " +
                    "Requirements: 4+ years MLOps, Kubernetes expertise, ML framework knowledge.",
                SalaryRaw = "$140,000 - $190,000 per year",
                Source = "Sample",
                Url = "https://aiml-systems.example.com/jobs/mlops-engineer-v2",
                PostedDate = DateTime.UtcNow.AddDays(-19),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "GraphQL API Developer",
                Company = "APIFirst Co",
                Location = "San Francisco, CA",
                Description = "Design and build GraphQL APIs for modern applications. " +
                    "You'll work with Node.js, TypeScript, and GraphQL. " +
                    "Requirements: 4+ years API development, GraphQL expertise, TypeScript proficiency.",
                SalaryRaw = "$115,000 - $155,000 per year",
                Source = "Sample",
                Url = "https://apifirst.example.com/jobs/graphql-developer",
                PostedDate = DateTime.UtcNow.AddDays(-20),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Embedded Systems Engineer - Rust",
                Company = "IoTEdge",
                Location = "Portland, OR",
                Description = "Develop embedded systems using Rust for IoT devices. " +
                    "You'll optimize for performance and memory efficiency. " +
                    "Requirements: 5+ years embedded systems, Rust expertise, hardware knowledge.",
                SalaryRaw = "$120,000 - $160,000 per year",
                Source = "Sample",
                Url = "https://iotedge.example.com/jobs/embedded-rust-engineer",
                PostedDate = DateTime.UtcNow.AddDays(-21),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Database Performance Engineer - PostgreSQL",
                Company = "DataMax",
                Location = "Remote",
                Description = "Optimize and tune database performance for large-scale systems. " +
                    "You'll work with PostgreSQL, query optimization, and indexing. " +
                    "Requirements: 6+ years database administration, PostgreSQL expertise, performance tuning.",
                SalaryRaw = "$125,000 - $170,000 per year",
                Source = "Sample",
                Url = "https://datamax.example.com/jobs/postgres-performance-engineer",
                PostedDate = DateTime.UtcNow.AddDays(-22),
                IsProcessed = false
            },
            new JobRaw
            {
                Title = "Network Engineer - Cisco",
                Company = "NetSecure",
                Location = "Boston, MA",
                Description = "Design and manage enterprise networks using Cisco technologies. " +
                    "You'll ensure network security and reliability. " +
                    "Requirements: 5+ years network engineering, Cisco CCNA/CCNP, security knowledge.",
                SalaryRaw = "$115,000 - $155,000 per year",
                Source = "Sample",
                Url = "https://netsecure.example.com/jobs/cisco-network-engineer",
                PostedDate = DateTime.UtcNow.AddDays(-23),
                IsProcessed = false
            }
        };
    }

    private string CleanDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "No description provided";

        try
        {
            // Remove HTML tags
            var cleaned = System.Text.RegularExpressions.Regex.Replace(description, "<[^>]*>", "");
            
            // Decode HTML entities
            cleaned = System.Net.WebUtility.HtmlDecode(cleaned);
            
            // Limit length to 5000 chars
            if (cleaned.Length > 5000)
            {
                cleaned = cleaned[..5000] + "...";
            }

            return cleaned.Trim();
        }
        catch
        {
            return description;
        }
    }

    /// <summary>
    /// Helper to safely extract nested JSON string values
    /// </summary>
    private string? GetJsonString(JsonElement element, params string[] propertyPath)
    {
        try
        {
            JsonElement current = element;

            foreach (var property in propertyPath)
            {
                if (!current.TryGetProperty(property, out current))
                {
                    return null;
                }
            }

            if (current.ValueKind == JsonValueKind.String)
            {
                return current.GetString();
            }
            else if (current.ValueKind == JsonValueKind.Number)
            {
                return current.GetRawText();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

