using Microsoft.AspNetCore.Mvc;
using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Infrastructure.Repositories;
using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Api.Controllers;

/// <summary>
/// API endpoints for job-related operations.
/// Provides functionality to retrieve and search job listings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobsController> _logger;
    private const int DefaultPageSize = 20;

    public JobsController(IJobRepository jobRepository, ILogger<JobsController> logger)
    {
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
            // Validate pagination parameters
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than 0");

            if (pageSize < 1 || pageSize > 100)
                return BadRequest("Page size must be between 1 and 100");

            _logger.LogInformation("Fetching jobs - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var jobs = await _jobRepository.GetAllAsync(pageNumber, pageSize);
            var totalCount = await _jobRepository.CountAsync();

            var result = new JobSearchResultDto
            {
                Jobs = MapToDto(jobs),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching jobs");
            return StatusCode(500, "An error occurred while fetching jobs");
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
            if (id <= 0)
                return BadRequest("Invalid job ID");

            _logger.LogInformation("Fetching job with ID: {JobId}", id);

            var job = await _jobRepository.GetByIdAsync(id);
            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", id);
                return NotFound();
            }

            return Ok(MapToDto(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching job with ID: {JobId}", id);
            return StatusCode(500, "An error occurred while fetching the job");
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
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than 0");

            if (pageSize < 1 || pageSize > 100)
                return BadRequest("Page size must be between 1 and 100");

            if (string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(location))
                return BadRequest("At least keyword or location must be provided");

            _logger.LogInformation("Searching jobs - Keyword: {Keyword}, Location: {Location}, Page: {PageNumber}",
                keyword, location, pageNumber);

            var jobs = await _jobRepository.SearchAsync(keyword ?? "", location, pageNumber, pageSize);
            // For search, we need to count matching results
            var allMatches = await _jobRepository.SearchAsync(keyword ?? "", location, 1, int.MaxValue);
            var totalCount = allMatches.Count;

            var result = new JobSearchResultDto
            {
                Jobs = MapToDto(jobs),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching jobs");
            return StatusCode(500, "An error occurred while searching jobs");
        }
    }

    private static List<JobRawDto> MapToDto(List<JobRaw> jobs)
    {
        return jobs.Select(MapToDto).ToList();
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
