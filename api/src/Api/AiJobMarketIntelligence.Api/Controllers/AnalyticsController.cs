using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AiJobMarketIntelligence.Api.Controllers;

/// <summary>
/// Analytics endpoints (aggregations over ingested data).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IJobRepository _jobRepository;

    private const int DefaultTake = 10;
    private const int MaxTake = 200;

    public AnalyticsController(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    /// <summary>
    /// Salary statistics computed from processed jobs (JobsProcessed table).
    /// Salary is computed per job as avg(min,max) when both exist, otherwise the non-null value.
    /// </summary>
    /// <param name="currency">Optional currency filter (e.g., USD).</param>
    /// <param name="location">Optional substring match on JobRaw.Location.</param>
    /// <param name="experienceLevel">Optional exact match on ExperienceLevel.</param>
    /// <param name="postedWithinDays">Optional filter on JobRaw.PostedDate within last N days.</param>
    [HttpGet("salary")]
    [ProducesResponseType(typeof(SalaryStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SalaryStatsDto>> GetSalaryStats(
        [FromQuery] string? currency,
        [FromQuery] string? location,
        [FromQuery] string? experienceLevel,
        [FromQuery] int? postedWithinDays)
    {
        if (postedWithinDays is < 0)
            return BadRequest("postedWithinDays must be >= 0");

        var (min, max, avg) = await _jobRepository.GetSalaryStatsAsync(currency, location, experienceLevel, postedWithinDays);

        return Ok(new SalaryStatsDto
        {
            Min = min,
            Max = max,
            Avg = avg,
            Currency = currency,
            Location = location,
            ExperienceLevel = experienceLevel,
            PostedWithinDays = postedWithinDays
        });
    }

    /// <summary>
    /// Daily ingestion counts based on JobRaw.CreatedAt.
    /// </summary>
    [HttpGet("ingestion/daily")]
    [ProducesResponseType(typeof(List<TimeSeriesPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TimeSeriesPointDto>>> GetIngestionDaily([FromQuery] int days = 30)
    {
        if (days < 1 || days > 3650) return BadRequest("days must be between 1 and 3650");

        var rows = await _jobRepository.GetIngestionDailyCountsAsync(days);
        return Ok(rows.Select(r => new TimeSeriesPointDto { Day = r.Day, Count = r.Count }).ToList());
    }

    /// <summary>
    /// Job counts by source.
    /// </summary>
    [HttpGet("breakdown/source")]
    [ProducesResponseType(typeof(List<KeyValueCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<KeyValueCountDto>>> GetCountsBySource([FromQuery] int take = DefaultTake)
    {
        if (take < 1 || take > MaxTake) return BadRequest($"take must be between 1 and {MaxTake}");

        var rows = await _jobRepository.GetCountsBySourceAsync(take);
        return Ok(rows.Select(r => new KeyValueCountDto { Key = r.Source, Count = r.Count }).ToList());
    }

    /// <summary>
    /// Job counts by location.
    /// </summary>
    [HttpGet("breakdown/location")]
    [ProducesResponseType(typeof(List<KeyValueCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<KeyValueCountDto>>> GetCountsByLocation([FromQuery] int take = DefaultTake)
    {
        if (take < 1 || take > MaxTake) return BadRequest($"take must be between 1 and {MaxTake}");

        var rows = await _jobRepository.GetCountsByLocationAsync(take);
        return Ok(rows.Select(r => new KeyValueCountDto { Key = r.Location, Count = r.Count }).ToList());
    }

    /// <summary>
    /// Processed-job counts by experience level.
    /// </summary>
    [HttpGet("breakdown/experience")]
    [ProducesResponseType(typeof(List<KeyValueCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<KeyValueCountDto>>> GetCountsByExperience([FromQuery] int take = DefaultTake)
    {
        if (take < 1 || take > MaxTake) return BadRequest($"take must be between 1 and {MaxTake}");

        var rows = await _jobRepository.GetCountsByExperienceLevelAsync(take);
        return Ok(rows.Select(r => new KeyValueCountDto { Key = r.ExperienceLevel, Count = r.Count }).ToList());
    }
}
