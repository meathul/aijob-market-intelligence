using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using AiJobMarketIntelligence.Api.Controllers;
using AiJobMarketIntelligence.Application.DTOs;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.UnitTests
{
    public class ApplicationsControllerTests
    {
        private readonly Mock<IJobApplicationRepository> _repoMock = new();
        private readonly ApplicationsController _controller;
        private const string TestUserId = "user-12345";

        public ApplicationsControllerTests()
        {
            _controller = new ApplicationsController(_repoMock.Object);
            SetupControllerUser(TestUserId);
        }

        private void SetupControllerUser(string? userId)
        {
            var claimsPrincipal = new ClaimsPrincipal();
            if (userId != null)
            {
                claimsPrincipal.AddIdentity(new ClaimsIdentity(new[]
                {
                    new Claim("sub", userId)
                }, "mock"));
            }

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetAppliedJobs_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(null);

            // Act
            var result = await _controller.GetAppliedJobs();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetAppliedJobs_AuthenticatedUser_ReturnsOkWithAppliedJobs()
        {
            // Arrange
            var jobsList = new List<JobRaw>
            {
                new()
                {
                    Id = 101,
                    Title = "AI Engineer",
                    Company = "DeepMind",
                    Location = "London",
                    Description = "Excellent AI job",
                    Source = "LinkedIn",
                    Url = "https://linkedin.com/ai",
                    PostedDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    IsProcessed = true,
                    JobSkills = new List<JobSkill>
                    {
                        new() { JobRawId = 101, SkillId = 1, Skill = new Skill { Id = 1, Name = "Python" } }
                    }
                }
            };
            _repoMock.Setup(r => r.GetAppliedJobsByUserIdAsync(TestUserId)).ReturnsAsync(jobsList);

            // Act
            var result = await _controller.GetAppliedJobs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dtos = Assert.IsType<List<JobRawDto>>(okResult.Value);
            Assert.Single(dtos);
            Assert.Equal(101, dtos[0].Id);
            Assert.Equal("AI Engineer", dtos[0].Title);
            Assert.Equal("DeepMind", dtos[0].Company);
            Assert.Single(dtos[0].Skills);
            Assert.Equal("Python", dtos[0].Skills[0].SkillName);
        }

        [Fact]
        public async Task Apply_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(null);

            // Act
            var result = await _controller.Apply(101);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Apply_JobDoesNotExistOrFailed_ReturnsBadRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.ApplyJobAsync(TestUserId, 101)).ReturnsAsync(false);

            // Act
            var result = await _controller.Apply(101);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = badRequestResult.Value?.GetType().GetProperty("message")?.GetValue(badRequestResult.Value)?.ToString();
            Assert.Equal("Failed to apply (job may not exist).", message);
        }

        [Fact]
        public async Task Apply_Success_ReturnsOk()
        {
            // Arrange
            _repoMock.Setup(r => r.ApplyJobAsync(TestUserId, 101)).ReturnsAsync(true);

            // Act
            var result = await _controller.Apply(101);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var success = (bool?)okResult.Value?.GetType().GetProperty("success")?.GetValue(okResult.Value);
            var message = okResult.Value?.GetType().GetProperty("message")?.GetValue(okResult.Value)?.ToString();
            Assert.True(success);
            Assert.Equal("Application saved.", message);
        }

        [Fact]
        public async Task Unapply_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(null);

            // Act
            var result = await _controller.Unapply(101);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Unapply_ApplicationNotFound_ReturnsBadRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.UnapplyJobAsync(TestUserId, 101)).ReturnsAsync(false);

            // Act
            var result = await _controller.Unapply(101);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = badRequestResult.Value?.GetType().GetProperty("message")?.GetValue(badRequestResult.Value)?.ToString();
            Assert.Equal("Application not found.", message);
        }

        [Fact]
        public async Task Unapply_Success_ReturnsOk()
        {
            // Arrange
            _repoMock.Setup(r => r.UnapplyJobAsync(TestUserId, 101)).ReturnsAsync(true);

            // Act
            var result = await _controller.Unapply(101);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var success = (bool?)okResult.Value?.GetType().GetProperty("success")?.GetValue(okResult.Value);
            var message = okResult.Value?.GetType().GetProperty("message")?.GetValue(okResult.Value)?.ToString();
            Assert.True(success);
            Assert.Equal("Application removed.", message);
        }
    }
}
