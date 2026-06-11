using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AiJobMarketIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private readonly IJobApplicationRepository _repo;

        public ApplicationsController(IJobApplicationRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Gets all jobs applied to by the authenticated user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<JobRawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAppliedJobs()
        {
            var userId = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var jobs = await _repo.GetAppliedJobsByUserIdAsync(userId);
            var dtos = jobs.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Creates a job application for the specified job ID.
        /// </summary>
        [HttpPost("{jobId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Apply(int jobId)
        {
            var userId = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _repo.ApplyJobAsync(userId, jobId);
            if (!success) return BadRequest(new { message = "Failed to apply (job may not exist)." });

            return Ok(new { success = true, message = "Application saved." });
        }

        /// <summary>
        /// Removes a job application for the specified job ID.
        /// </summary>
        [HttpDelete("{jobId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Unapply(int jobId)
        {
            var userId = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _repo.UnapplyJobAsync(userId, jobId);
            if (!success) return BadRequest(new { message = "Application not found." });

            return Ok(new { success = true, message = "Application removed." });
        }

        private static JobRawDto MapToDto(JobRaw r) => new()
        {
            Id = r.Id,
            Title = r.Title,
            Company = r.Company,
            Location = r.Location,
            Description = r.Description,
            SalaryRaw = r.SalaryRaw,
            Source = r.Source,
            Url = r.Url,
            PostedDate = r.PostedDate,
            CreatedAt = r.CreatedAt,
            IsProcessed = r.IsProcessed,
            Skills = r.JobSkills?.Select(js => new JobSkillDto
            {
                SkillName = js.Skill?.Name
            }).ToList() ?? new List<JobSkillDto>()
        };
       }
}
