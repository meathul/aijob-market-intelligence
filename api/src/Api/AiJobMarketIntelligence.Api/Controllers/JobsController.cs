using Microsoft.AspNetCore.Mvc;
using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.Interfaces.Services;

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
    private readonly ILogger<JobsController> _logger;
    private const int DefaultPageSize = 20;

    public JobsController(IJobQueryService jobQueryService, ILogger<JobsController> logger)
    {
        _jobQueryService = jobQueryService;
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
}
