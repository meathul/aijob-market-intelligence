using AiJobMarketIntelligence.Application.DTOs.Recommendations;
using AiJobMarketIntelligence.Application.Interfaces.Services.Recommendations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiJobMarketIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class JobsRecommendationsController : ControllerBase
{
    private readonly IJobRecommendationService _svc;

    public JobsRecommendationsController(IJobRecommendationService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    [ProducesResponseType(typeof(JobRecommendationsResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobRecommendationsResultDto>> Get([FromQuery] int take = 20)
    {
        var userId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var res = await _svc.GetRecommendedJobsAsync(userId, take);
        return Ok(res);
    }
}
