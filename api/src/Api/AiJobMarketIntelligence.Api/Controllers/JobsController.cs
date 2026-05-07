using Microsoft.AspNetCore.Mvc;
using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.Interfaces.Services;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;

namespace AiJobMarketIntelligence.Api.Controllers;

/// <summary>
/// API endpoints for job-related operations.
/// Provides functionality to retrieve and search job listings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobQueryService _jobQueryService;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobsController> _logger;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public JobsController(
        IJobQueryService jobQueryService,
        IJobRepository jobRepository,
        ILogger<JobsController> logger)
    {
        _jobQueryService = jobQueryService;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all jobs with pagination support.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based). Default: 1</param>
    /// <param name="pageSize">Page size. Default: 20. Max: 100</param>
    [HttpGet]
    [ProducesResponseType(typeof(JobSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobSearchResultDto>> GetJobs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        try
        {
            var result = await _jobQueryService.GetJobsAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid pagination parameters");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a specific job by ID.
    /// </summary>
    /// <param name="id">Job ID</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(JobRawDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobRawDto>> GetJobById([FromRoute] int id)
    {
        try
        {
            var job = await _jobQueryService.GetJobByIdAsync(id);
            return job is null ? NotFound() : Ok(job);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid job id");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Searches for jobs by keyword and/or location.
    /// </summary>
    /// <param name="keyword">Search keyword (searches in title, description, company)</param>
    /// <param name="location">Filter by location</param>
    /// <param name="pageNumber">Page number (1-based). Default: 1</param>
    /// <param name="pageSize">Page size. Default: 20. Max: 100</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(JobSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobSearchResultDto>> SearchJobs(
        [FromQuery] string? keyword,
        [FromQuery] string? location,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        try
        {
            var result = await _jobQueryService.SearchJobsAsync(keyword, location, pageNumber, pageSize);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid search query");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets jobs marked as processed.
    /// </summary>
    [HttpGet("processed")]
    [ProducesResponseType(typeof(JobSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobSearchResultDto>> GetProcessedJobs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        if (pageNumber < 1) return BadRequest("pageNumber must be >= 1");
        if (pageSize < 1 || pageSize > MaxPageSize) return BadRequest($"pageSize must be between 1 and {MaxPageSize}");

        var jobs = await _jobRepository.GetProcessedAsync(pageNumber, pageSize);
        var totalCount = await _jobRepository.CountProcessedAsync();

        return Ok(new JobSearchResultDto
        {
            Jobs = jobs.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Gets jobs not yet processed.
    /// </summary>
    [HttpGet("unprocessed")]
    [ProducesResponseType(typeof(List<JobRawDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JobRawDto>>> GetUnprocessedJobs()
    {
        var jobs = await _jobRepository.GetUnprocessedAsync();
        return Ok(jobs.Select(MapToDto).ToList());
    }

    private static JobRawDto MapToDto(AiJobMarketIntelligence.Domain.Entities.JobRaw job)
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
