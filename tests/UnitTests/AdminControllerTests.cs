using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AiJobMarketIntelligence.Api.Controllers;
using AiJobMarketIntelligence.Application.Services;

namespace AiJobMarketIntelligence.UnitTests
{
    public class AdminControllerTests
    {
        private readonly Mock<IJobIngestionService> _jobIngestionServiceMock = new();
        private readonly Mock<ILogger<AdminController>> _loggerMock = new();
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _controller = new AdminController(_jobIngestionServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task TriggerJobFetch_Success_ReturnsOkWithStats()
        {
            // Arrange
            const int expectedJobsAdded = 15;
            _jobIngestionServiceMock.Setup(s => s.IngestJobsAsync()).ReturnsAsync(expectedJobsAdded);

            // Act
            var result = await _controller.TriggerJobFetch();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Using reflection/dynamic to inspect anonymous types
            var successValue = okResult.Value.GetType().GetProperty("success")?.GetValue(okResult.Value);
            var messageValue = okResult.Value.GetType().GetProperty("message")?.GetValue(okResult.Value);
            var jobsAddedValue = okResult.Value.GetType().GetProperty("jobsAdded")?.GetValue(okResult.Value);

            Assert.Equal(true, successValue);
            Assert.Contains("completed successfully", messageValue?.ToString());
            Assert.Equal(expectedJobsAdded, jobsAddedValue);

            _jobIngestionServiceMock.Verify(s => s.IngestJobsAsync(), Times.Once);
        }

        [Fact]
        public async Task TriggerJobFetch_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            const string errorMessage = "Simulated network failure";
            _jobIngestionServiceMock.Setup(s => s.IngestJobsAsync()).ThrowsAsync(new Exception(errorMessage));

            // Act
            var result = await _controller.TriggerJobFetch();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);

            var successValue = statusCodeResult.Value.GetType().GetProperty("success")?.GetValue(statusCodeResult.Value);
            var messageValue = statusCodeResult.Value.GetType().GetProperty("message")?.GetValue(statusCodeResult.Value);
            var errorValue = statusCodeResult.Value.GetType().GetProperty("error")?.GetValue(statusCodeResult.Value);

            Assert.Equal(false, successValue);
            Assert.Equal("An error occurred during job ingestion", messageValue?.ToString());
            Assert.Equal(errorMessage, errorValue?.ToString());

            _jobIngestionServiceMock.Verify(s => s.IngestJobsAsync(), Times.Once);
        }
    }
}
