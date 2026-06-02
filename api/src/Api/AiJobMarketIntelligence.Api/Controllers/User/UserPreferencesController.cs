using AiJobMarketIntelligence.Application.DTOs.UserPreferences;
using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Domain.Entities.UserPreferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiJobMarketIntelligence.Api.Controllers.User;

[ApiController]
[Route("api/user/preferences")]
[Authorize]
public sealed class UserPreferencesController : ControllerBase
{
    private readonly IUserJobPreferencesRepository _repo;

    public UserPreferencesController(IUserJobPreferencesRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserJobPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var userId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var prefs = await _repo.GetByUserIdAsync(userId);
        if (prefs is null)
            return Ok(new UserJobPreferencesDto());

        return Ok(ToDto(prefs));
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserJobPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromBody] UserJobPreferencesDto request)
    {
        if (request.PreferredSalaryMin is not null && request.PreferredSalaryMin < 0)
            return BadRequest(new { message = "PreferredSalaryMin must be >= 0" });

        if (request.PreferredSalaryMax is not null && request.PreferredSalaryMax < 0)
            return BadRequest(new { message = "PreferredSalaryMax must be >= 0" });

        if (request.PreferredSalaryMin is not null && request.PreferredSalaryMax is not null &&
            request.PreferredSalaryMin > request.PreferredSalaryMax)
        {
            return BadRequest(new { message = "PreferredSalaryMin must be <= PreferredSalaryMax" });
        }

        var userId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var entity = new UserJobPreferences
        {
            UserId = userId,
            Location = request.Location?.Trim(),
            PreferredSalaryMin = request.PreferredSalaryMin,
            PreferredSalaryMax = request.PreferredSalaryMax,
            PreferredJobTitle = request.PreferredJobTitle?.Trim(),
            WorkMode = request.WorkMode?.Trim(),
            SkillsText = request.SkillsText?.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        var saved = await _repo.UpsertAsync(entity);
        return Ok(ToDto(saved));
    }

    private static UserJobPreferencesDto ToDto(UserJobPreferences x) => new()
    {
        Location = x.Location,
        PreferredSalaryMin = x.PreferredSalaryMin,
        PreferredSalaryMax = x.PreferredSalaryMax,
        PreferredJobTitle = x.PreferredJobTitle,
        WorkMode = x.WorkMode,
        SkillsText = x.SkillsText
    };
}
