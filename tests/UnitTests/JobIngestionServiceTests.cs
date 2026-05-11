using System.Threading.Tasks;
using Moq;
using Xunit;
using AiJobMarketIntelligence.Application.Services;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Services.Providers;
using AiJobMarketIntelligence.Domain.Entities;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.UnitTests
{
    public class JobIngestionServiceTests
    {
        private readonly Mock<IJobRepository> _jobRepoMock = new();
        private readonly Mock<IJobProvider> _jobProviderMock = new();
        private readonly Mock<ILogger<JobIngestionService>> _loggerMock = new();
        private readonly JobIngestionService _service;

        public JobIngestionServiceTests()
        {
            _service = new JobIngestionService(_jobRepoMock.Object, _jobProviderMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task IngestJobsAsync_RunsSuccessfully()
        {
            var jobs = new List<JobRaw>
            {
                new JobRaw { Id = 1, Url = "http://job1", Title = "Job 1", Company = "Company 1", Description = "Desc 1", Location = "Location 1", Source = "Test" },
                new JobRaw { Id = 2, Url = "http://job2", Title = "Job 2", Company = "Company 2", Description = "Desc 2", Location = "Location 2", Source = "Test" }
            };
            _jobProviderMock.Setup(p => p.FetchJobsAsync()).ReturnsAsync(jobs);
            _jobRepoMock.Setup(r => r.ExistsByUrlAsync(It.IsAny<string>())).ReturnsAsync(false);
            _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<JobRaw>())).Returns(Task.CompletedTask);

            // Just verify the method runs without throwing an exception
            await _service.IngestJobsAsync();
            Assert.True(true);
        }
    }
}
