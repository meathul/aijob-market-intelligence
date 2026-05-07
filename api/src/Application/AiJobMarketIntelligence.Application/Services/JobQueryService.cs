using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Interfaces.Services;
using AiJobMarketIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.Application.Services;

public class JobQueryService : IJobQueryService
{
    private const int MaxPageSize = 100;

    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobQueryService> _logger;

    public JobQueryService(IJobRepository jobRepository, ILogger<JobQueryService> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task<JobSearchResultDto> GetJobsAsync(int pageNumber, int pageSize)
    {
        ValidatePagination(pageNumber, pageSize);

        _logger.LogInformation("Fetching jobs - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        var jobs = await _jobRepository.GetAllAsync(pageNumber, pageSize);
        var totalCount = await _jobRepository.CountAsync();

        return new JobSearchResultDto
        {
            Jobs = jobs.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<JobRawDto?> GetJobByIdAsync(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Invalid job ID");

        _logger.LogInformation("Fetching job with ID: {JobId}", id);

        var job = await _jobRepository.GetByIdAsync(id);
        return job is null ? null : MapToDto(job);
    }

    public async Task<JobSearchResultDto> SearchJobsAsync(string? keyword, string? location, int pageNumber, int pageSize)
    {
        ValidatePagination(pageNumber, pageSize);

        if (string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("At least keyword or location must be provided");

        _logger.LogInformation(
            "Searching jobs - Keyword: {Keyword}, Location: {Location}, Page: {PageNumber}",
            keyword,
            location,
            pageNumber);

        // Repository handles null/empty filtering consistently.
        var jobs = await _jobRepository.SearchAsync(keyword ?? string.Empty, location, pageNumber, pageSize);
        var totalCount = await _jobRepository.CountSearchAsync(keyword, location);

        return new JobSearchResultDto
        {
            Jobs = jobs.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0");
        if (pageSize < 1 || pageSize > MaxPageSize) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100");
    }

    private static JobRawDto MapToDto(JobRaw job)
    {
        return new JobRawDto
        {
            Id = job.Id,
            Title = job.Title,
            Company = job.Company,
            Location = job.Location,
            Description = job.Description,
            SalaryRaw = job.SalaryRaw,
            Source = job.Source,
            Url = job.Url,
            PostedDate = job.PostedDate,
            CreatedAt = job.CreatedAt,
            IsProcessed = job.IsProcessed,
            Skills = job.JobSkills?.Select(js => new JobSkillDto
            {
                SkillId = js.SkillId,
                SkillName = js.Skill.Name
            }).ToList() ?? new()
        };
    }
}
