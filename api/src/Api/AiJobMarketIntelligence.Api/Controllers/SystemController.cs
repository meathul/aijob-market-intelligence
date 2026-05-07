using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AiJobMarketIntelligence.Api.Controllers;

/// <summary>
/// System/health endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IJobRepository _jobRepository;
    private readonly ISkillRepository _skillRepository;

    public SystemController(IJobRepository jobRepository, ISkillRepository skillRepository)
    {
        _jobRepository = jobRepository;
        _skillRepository = skillRepository;
    }

    /// <summary>
    /// Simple liveness probe.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "ok", timestampUtc = DateTime.UtcNow });
    }

    /// <summary>
    /// Basic stats about ingested data.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(SystemStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemStatsDto>> Stats()
    {
        var totalJobs = await _jobRepository.CountAsync();
        var processedJobs = await _jobRepository.CountProcessedAsync();
        var unprocessedJobs = totalJobs - processedJobs;

        var totalSkills = (await _skillRepository.GetAllAsync()).Count;

        return Ok(new SystemStatsDto
        {
            TotalJobs = totalJobs,
            ProcessedJobs = processedJobs,
            UnprocessedJobs = unprocessedJobs,
            TotalSkills = totalSkills,
            TimestampUtc = DateTime.UtcNow
        });
    }
}
