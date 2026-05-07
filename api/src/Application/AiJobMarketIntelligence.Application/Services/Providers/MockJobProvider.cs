using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Application.Services.Providers;
using System.Text.Json;

namespace AiJobMarketIntelligence.Application.Services;

/// <summary>
/// Mock job provider that returns sample job data.
/// Used for testing and demonstration purposes.
/// </summary>
public class MockJobProvider : IJobProvider
{
    public Task<List<JobRaw>> FetchJobsAsync()
    {
        var jobs = new List<JobRaw>
        {
            new()
            {
                Title = "Senior Backend Engineer",
                Company = "TechCorp Inc.",
                Location = "San Francisco, CA",
                Description = "We are looking for an experienced backend engineer with 5+ years of experience in .NET and cloud technologies. You will work on scalable distributed systems.",
                SalaryRaw = "$150,000 - $200,000",
                Source = "MockAPI",
                Url = "https://mock-api.example.com/jobs/1",
                PostedDate = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            },
            new()
            {
                Title = "Full Stack Developer",
                Company = "WebSolutions Ltd.",
                Location = "New York, NY",
                Description = "Join our dynamic team as a Full Stack Developer. Experience with React, Node.js, and PostgreSQL required. Work on modern web applications.",
                SalaryRaw = "$120,000 - $160,000",
                Source = "MockAPI",
                Url = "https://mock-api.example.com/jobs/2",
                PostedDate = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            },
            new()
            {
                Title = "DevOps Engineer",
                Company = "CloudSystems Co.",
                Location = "Seattle, WA",
                Description = "We need a skilled DevOps engineer to manage our Kubernetes infrastructure. Experience with Docker, CI/CD pipelines, and AWS is essential.",
                SalaryRaw = "$140,000 - $180,000",
                Source = "MockAPI",
                Url = "https://mock-api.example.com/jobs/3",
                PostedDate = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            },
            new()
            {
                Title = "Data Engineer",
                Company = "DataInsights Corp.",
                Location = "Boston, MA",
                Description = "Looking for a Data Engineer with expertise in Apache Spark, Python, and big data technologies. Build and maintain data pipelines.",
                SalaryRaw = "$130,000 - $170,000",
                Source = "MockAPI",
                Url = "https://mock-api.example.com/jobs/4",
                PostedDate = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            },
            new()
            {
                Title = "Machine Learning Engineer",
                Company = "AI Innovations Inc.",
                Location = "Austin, TX",
                Description = "We are seeking an ML engineer to develop and deploy machine learning models. Strong background in Python, TensorFlow, and deep learning.",
                SalaryRaw = "$160,000 - $210,000",
                Source = "MockAPI",
                Url = "https://mock-api.example.com/jobs/5",
                PostedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            }
        };

        return Task.FromResult(jobs);
    }
}
