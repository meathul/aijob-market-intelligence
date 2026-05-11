using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace AiJobMarketIntelligence.Application.Services.Skills;

/// <summary>
/// Skill extraction service using OpenAI ChatGPT API.
/// Provides intelligent extraction of technical skills from job descriptions.
/// </summary>
public class OpenAiSkillExtractionService : ISkillExtractionService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAiSkillExtractionService> _logger;

    public OpenAiSkillExtractionService(string apiKey, ILogger<OpenAiSkillExtractionService> logger)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "OpenAI API key is required");
        }

        _chatClient = new ChatClient("gpt-4o-mini", apiKey);
        _logger = logger;
    }

    /// <summary>
    /// Extracts technical skills from job description and title using OpenAI ChatGPT.
    /// </summary>
    /// <param name="jobDescription">The job description text to analyze</param>
    /// <param name="jobTitle">The job title</param>
    /// <returns>List of extracted skills</returns>
    public async Task<List<string>> ExtractSkillsAsync(string jobDescription, string jobTitle)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jobDescription) && string.IsNullOrWhiteSpace(jobTitle))
            {
                _logger.LogWarning("Both job description and title are empty");
                return new List<string>();
            }

            // Combine description and title for analysis
            var combinedText = $"Job Title: {jobTitle}\n\nDescription: {jobDescription}";

            // Create the prompt for ChatGPT
            var systemPrompt = @"You are an expert technical recruiter skilled at identifying technical and professional skills from job descriptions.

Extract ALL technical skills, frameworks, tools, databases, cloud platforms, methodologies, and programming languages mentioned in the job posting.

Return ONLY a comma-separated list of skills. No explanations, no numbering, just skills separated by commas.
Examples of skills to look for:
- Programming languages (C#, Python, JavaScript, Java, Go, Rust, etc.)
- Frontend frameworks (React, Angular, Vue, Next.js, etc.)
- Backend frameworks (.NET, Spring, Django, Laravel, Node.js, etc.)
- Databases (MySQL, PostgreSQL, MongoDB, Redis, SQL Server, etc.)
- Cloud platforms (AWS, Azure, Google Cloud, etc.)
- DevOps tools (Docker, Kubernetes, CI/CD, Jenkins, GitLab CI, etc.)
- Testing frameworks (xUnit, Jest, Pytest, Selenium, etc.)
- Version control (Git, GitHub, GitLab, etc.)
- Methodologies (Agile, Scrum, TDD, DDD, etc.)
- Other tools and platforms relevant to the job

Be thorough but return only legitimate technical skills that are clearly mentioned or strongly implied.";

            var userPrompt = $"Extract technical skills from this job posting:\n\n{combinedText}";

            // Call OpenAI API
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            _logger.LogInformation("Calling OpenAI ChatGPT for skill extraction");
            var response = await _chatClient.CompleteChatAsync(messages);

            // Parse the response
            var skillsText = response.Value.Content[0].Text;
            _logger.LogDebug($"OpenAI Response: {skillsText}");

            // Split and clean up skills
            var skills = skillsText
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 1) // Filter out single characters
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation($"Extracted {skills.Count} skills: {string.Join(", ", skills)}");

            return skills;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting skills from job description");
            // Return empty list instead of throwing to allow job processing to continue
            return new List<string>();
        }
    }
}
