using Microsoft.AspNetCore.Mvc;
using AiJobMarketIntelligence.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace AiJobMarketIntelligence.Api.Controllers;

/// <summary>
/// Admin endpoints for managing job ingestion and system operations.
/// These endpoints should be protected by authentication in production.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IJobIngestionService _jobIngestionService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IJobIngestionService jobIngestionService, ILogger<AdminController> logger)
    {
        _jobIngestionService = jobIngestionService;
        _logger = logger;
    }

    /// <summary>
    /// Manually triggers the job ingestion process.
    /// In production, this endpoint should be secured with authentication/authorization.
    /// </summary>
    [HttpPost("trigger-fetch")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TriggerJobFetch()
    {
        try
        {
            _logger.LogInformation("Manual job ingestion triggered via API");

            var jobsAdded = await _jobIngestionService.IngestJobsAsync();

            var response = new
            {
                success = true,
                message = $"Job ingestion completed successfully",
                jobsAdded = jobsAdded,
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual job ingestion trigger");

            var errorResponse = new
            {
                success = false,
                message = "An error occurred during job ingestion",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }
}
