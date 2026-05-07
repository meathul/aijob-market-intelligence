using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AiJobMarketIntelligence.Api.Controllers;

/// <summary>
/// API endpoints for skill-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly ISkillRepository _skillRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<SkillsController> _logger;

    private const int DefaultTopTake = 20;
    private const int MaxTopTake = 200;

    public SkillsController(
        ISkillRepository skillRepository,
        IJobRepository jobRepository,
        ILogger<SkillsController> logger)
    {
        _skillRepository = skillRepository;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all known skills.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetAllSkills()
    {
        var skills = await _skillRepository.GetAllAsync();
        return Ok(skills.Select(s => s.Name).OrderBy(n => n).ToList());
    }

    /// <summary>
    /// Gets top skills by frequency across ingested jobs.
    /// </summary>
    /// <param name="take">Number of skills to return. Default: 20. Max: 200</param>
    [HttpGet("top")]
    [ProducesResponseType(typeof(List<SkillCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<SkillCountDto>>> GetTopSkills([FromQuery] int take = DefaultTopTake)
    {
        if (take < 1 || take > MaxTopTake)
            return BadRequest($"take must be between 1 and {MaxTopTake}");

        var rows = await _jobRepository.GetTopSkillsAsync(take);

        return Ok(rows.Select(r => new SkillCountDto { Skill = r.Skill, Count = r.Count }).ToList());
    }
}
