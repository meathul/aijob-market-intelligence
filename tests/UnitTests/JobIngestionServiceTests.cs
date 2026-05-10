using System.Threading.Tasks;
using Moq;
using Xunit;
using AiJobMarketIntelligence.Application.Services;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Application.Services.Providers;
using AiJobMarketIntelligence.Domain.Entities;
using System.Collections.Generic;
using System.Threading;

namespace AiJobMarketIntelligence.UnitTests
{
    public class JobIngestionServiceTests
    {
        private readonly Mock<IJobRepository> _jobRepoMock = new();
        private readonly Mock<IJobProvider> _jobProviderMock = new();
        private readonly JobIngestionService _service;

        public JobIngestionServiceTests()
        {
            _service = new JobIngestionService(_jobRepoMock.Object, _jobProviderMock.Object);
        }

        [Fact]
        public async Task IngestJobsAsync_AddsNewJobs()
        {
            var jobs = new List<JobRaw>
            {
                new JobRaw { Id = 1, Url = "http://job1" },
                new JobRaw { Id = 2, Url = "http://job2" }
            };
            _jobProviderMock.Setup(p => p.FetchJobsAsync()).ReturnsAsync(jobs);
            _jobRepoMock.Setup(r => r.ExistsByUrlAsync(It.IsAny<string>())).ReturnsAsync(false);
            _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<JobRaw>())).Returns(Task.CompletedTask);

            await _service.IngestJobsAsync();

            _jobRepoMock.Verify(r => r.AddAsync(It.IsAny<JobRaw>()), Times.Exactly(2));
        }
    }
}
