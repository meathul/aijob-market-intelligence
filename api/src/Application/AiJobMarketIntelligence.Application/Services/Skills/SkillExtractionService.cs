using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.Application.Services.Skills;

public class SkillExtractionService : ISkillExtractionService
{
    private readonly ILogger<SkillExtractionService> _logger;

    // Comprehensive list of common technical and professional skills
    private static readonly List<string> KnownSkills = new()
    {
        // Programming Languages
        "C#", "Python", "JavaScript", "TypeScript", "Java", "C++", "C", "Ruby", "PHP", "Go", "Rust", "Kotlin",
        "Swift", "Objective-C", "R", "MATLAB", "Scala", "Groovy", "Perl", "Haskell", "Clojure",
        
        // Frontend
        "React", "Angular", "Vue", "Vue.js", "Svelte", "Next.js", "HTML", "CSS", "Bootstrap", "Tailwind",
        "jQuery", "Backbone", "Ember", "Polymer", "Web Components",
        
        // Backend
        "ASP.NET", "ASP.NET Core", ".NET", ".NET Core", "Node.js", "Express", "Django", "Flask", "Spring",
        "Spring Boot", "Laravel", "Symfony", "Rails", "Ruby on Rails", "NestJS", "FastAPI",
        
        // Databases
        "SQL", "MySQL", "PostgreSQL", "MongoDB", "Redis", "Cassandra", "Oracle", "SQL Server",
        "ElasticSearch", "DynamoDB", "Firestore", "Firebase", "CosmosDB",
        
        // Cloud & DevOps
        "AWS", "Azure", "Google Cloud", "GCP", "Docker", "Kubernetes", "CI/CD", "Jenkins", "GitLab CI",
        "GitHub Actions", "Terraform", "CloudFormation", "Ansible", "Heroku",
        
        // Version Control
        "Git", "GitHub", "GitLab", "Bitbucket", "SVN", "Mercurial",
        
        // Testing
        "Unit Testing", "Integration Testing", "Jest", "Mocha", "Chai", "NUnit", "xUnit", "Pytest",
        "Selenium", "Cypress", "Postman", "JUnit", "TestNG",
        
        // Tools & Platforms
        "Visual Studio", "VS Code", "IntelliJ", "Eclipse", "Xcode", "Vim", "Sublime",
        "JIRA", "Slack", "Confluence", "Trello", "Asana", "Monday.com",
        
        // Methodologies
        "Agile", "Scrum", "Kanban", "Waterfall", "Lean", "DevOps", "TDD", "BDD", "DDD",
        
        // Big Data & AI/ML
        "Machine Learning", "Deep Learning", "TensorFlow", "PyTorch", "Scikit-learn", "Keras",
        "Spark", "Hadoop", "Hive", "Apache Kafka", "Airflow", "Pandas", "NumPy",
        
        // Other
        "REST API", "GraphQL", "SOAP", "Microservices", "SOA", "ECS", "S3", "Lambda",
        "API Development", "System Design", "Data Structures", "Algorithms", "Linux", "Unix",
        "Windows Server", "Networking", "Security", "OAuth", "JWT", "SSL", "TLS"
    };

    public SkillExtractionService(ILogger<SkillExtractionService> logger)
    {
        _logger = logger;
    }

    public Task<List<string>> ExtractSkillsAsync(string jobDescription, string jobTitle)
    {
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (string.IsNullOrWhiteSpace(jobDescription) && string.IsNullOrWhiteSpace(jobTitle))
                return Task.FromResult(new List<string>());

            var combinedText = $"{jobTitle} {jobDescription}";
            var lowerText = combinedText.ToLower();

            // Match known skills
            foreach (var skill in KnownSkills)
            {
                // Use word boundaries to match whole words only
                var pattern = $@"\b{Regex.Escape(skill)}\b";
                if (Regex.IsMatch(lowerText, pattern, RegexOptions.IgnoreCase))
                {
                    skills.Add(skill);
                }
            }

            // Extract version-specific technologies (e.g., "Python 3.9", "Node.js 16")
            var versions = Regex.Matches(combinedText, @"\b(\w+)\s+\d+\.\d+", RegexOptions.IgnoreCase);
            foreach (Match match in versions)
            {
                var tech = match.Groups[1].Value;
                if (KnownSkills.Any(s => s.Equals(tech, StringComparison.OrdinalIgnoreCase)))
                {
                    skills.Add(tech);
                }
            }

            _logger.LogInformation($"Extracted {skills.Count} skills from job description");
            return Task.FromResult(skills.OrderBy(s => s).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error extracting skills: {ex.Message}");
            return Task.FromResult(new List<string>());
        }
    }
}
