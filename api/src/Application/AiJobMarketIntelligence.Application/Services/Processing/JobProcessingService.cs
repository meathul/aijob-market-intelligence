using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Services.Salary;
using AiJobMarketIntelligence.Application.Services.Skills;
using AiJobMarketIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.Application.Services.Processing;

public sealed class JobProcessingService : IJobProcessingService
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobProcessedRepository _jobProcessedRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISalaryParserService _salaryParser;
    private readonly ISkillExtractionService _skillExtractor;
    private readonly ILogger<JobProcessingService> _logger;

    public JobProcessingService(
        IJobRepository jobRepository,
        IJobProcessedRepository jobProcessedRepository,
        ISkillRepository skillRepository,
        ISalaryParserService salaryParser,
        ISkillExtractionService skillExtractor,
        ILogger<JobProcessingService> logger)
    {
        _jobRepository = jobRepository;
        _jobProcessedRepository = jobProcessedRepository;
        _skillRepository = skillRepository;
        _salaryParser = salaryParser;
        _skillExtractor = skillExtractor;
        _logger = logger;
    }

    public async Task<int> ProcessPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _jobRepository.GetUnprocessedAsync();
        if (pending.Count == 0)
        {
            _logger.LogInformation("No unprocessed jobs found");
            return 0;
        }

        var processedCount = 0;

        foreach (var raw in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Salary parsing from raw field and/or description.
                var salary = _salaryParser.Parse(raw.SalaryRaw, raw.Description);
                _logger.LogDebug(
                    "Salary parse for job {JobRawId}: raw='{SalaryRaw}' => min={Min}, max={Max}, cur={Cur}, period={Period}",
                    raw.Id,
                    raw.SalaryRaw,
                    salary.SalaryMin,
                    salary.SalaryMax,
                    salary.Currency,
                    salary.SalaryPeriod);

                // Experience level (heuristic) from description/title.
                var exp = InferExperienceLevel(raw.Title, raw.Description);

                var processed = new JobProcessed
                {
                    JobRawId = raw.Id,
                    SalaryMin = salary.SalaryMin,
                    SalaryMax = salary.SalaryMax,
                    Currency = salary.Currency,
                    SalaryPeriod = salary.SalaryPeriod,
                    ExperienceLevel = exp,
                    ProcessedAt = DateTime.UtcNow
                };

                await _jobProcessedRepository.UpsertByRawJobIdAsync(processed);

                // Extract skills from job description and title (ensure we provide useful text)
                var title = raw.Title ?? string.Empty;
                var desc = raw.Description ?? string.Empty;
                if (string.IsNullOrWhiteSpace(desc) && !string.IsNullOrWhiteSpace(title))
                    desc = title;
                if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(desc))
                    title = desc.Length > 80 ? desc[..80] : desc;

                var skills = await _skillExtractor.ExtractSkillsAsync(desc, title);

                // Filter obvious bad outputs (paragraphs, etc.)
                skills = skills
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 255)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (skills.Count > 0)
                {
                    foreach (var skillName in skills)
                    {
                        await _skillRepository.AddJobSkillAsync(raw.Id, skillName);
                        _logger.LogDebug("Skill '{Skill}' extracted for job {JobRawId}", skillName, raw.Id);
                    }
                }

                raw.IsProcessed = true;
                await _jobRepository.UpdateAsync(raw);

                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process raw job {JobRawId}", raw.Id);
            }
        }

        await _jobProcessedRepository.SaveAsync();
        await _jobRepository.SaveAsync();
        await _skillRepository.SaveAsync();

        _logger.LogInformation("Processed {Count} jobs", processedCount);
        return processedCount;
    }

    private static string? InferExperienceLevel(string? title, string? description)
    {
        var t = (title ?? string.Empty).ToLowerInvariant();
        var d = (description ?? string.Empty).ToLowerInvariant();
        var text = t + "\n" + d;

        if (text.Contains("intern")) return "Intern";
        if (text.Contains("junior") || text.Contains("jr.")) return "Junior";
        if (text.Contains("mid-level") || text.Contains("mid level")) return "Mid";
        if (text.Contains("senior") || text.Contains("sr.")) return "Senior";
        if (text.Contains("staff") || text.Contains("principal") || text.Contains("lead")) return "Lead";

        return null;
    }
}
