using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using AiJobMarketIntelligence.Application.Services.Processing;
using AiJobMarketIntelligence.Application.Services.Salary;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.UnitTests
{
    public class JobProcessingServiceTests
    {
        private readonly Mock<IJobProcessedRepository> _jobProcessedRepoMock = new();
        private readonly Mock<IJobRepository> _jobRepoMock = new();
        private readonly Mock<ISalaryParserService> _salaryParserMock = new();
        private readonly Mock<ILogger<JobProcessingService>> _loggerMock = new();
        private readonly JobProcessingService _service;

        public JobProcessingServiceTests()
        {
            _service = new JobProcessingService(_jobRepoMock.Object, _jobProcessedRepoMock.Object, _salaryParserMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessPendingJobsAsync_ProcessesJobs()
        {
            _jobRepoMock.Setup(r => r.GetUnprocessedAsync()).ReturnsAsync(new System.Collections.Generic.List<JobRaw> {
                new JobRaw { Id = 1, SalaryRaw = "$100k - $120k", Url = "http://test.com", Title = "Test Job", Company = "Test", Description = "Test", Location = "Test", Source = "Test" }
            });
            _salaryParserMock.Setup(s => s.Parse(It.IsAny<string>(), It.IsAny<string>())).Returns(new SalaryParseResult { SalaryMin = 100000, SalaryMax = 120000, SalaryPeriod = SalaryPeriod.Yearly });
            _jobProcessedRepoMock.Setup(r => r.UpsertByRawJobIdAsync(It.IsAny<JobProcessed>())).Returns(Task.CompletedTask);

            var result = await _service.ProcessPendingJobsAsync(CancellationToken.None);

            Assert.True(result > 0);
            _jobProcessedRepoMock.Verify(r => r.UpsertByRawJobIdAsync(It.IsAny<JobProcessed>()), Times.Once);
        }
    }
}
