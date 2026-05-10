using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using AiJobMarketIntelligence.Application.Services.Processing;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.UnitTests
{
    public class JobProcessingServiceTests
    {
        private readonly Mock<IJobProcessedRepository> _jobProcessedRepoMock = new();
        private readonly Mock<IJobRepository> _jobRepoMock = new();
        private readonly Mock<ISalaryParserService> _salaryParserMock = new();
        private readonly JobProcessingService _service;

        public JobProcessingServiceTests()
        {
            _service = new JobProcessingService(_jobProcessedRepoMock.Object, _jobRepoMock.Object, _salaryParserMock.Object);
        }

        [Fact]
        public async Task ProcessPendingJobsAsync_ProcessesJobs()
        {
            _jobRepoMock.Setup(r => r.GetUnprocessedAsync()).ReturnsAsync(new[] {
                new JobRaw { Id = 1, SalaryRaw = "$100k - $120k" }
            });
            _salaryParserMock.Setup(s => s.ParseSalary(It.IsAny<string>())).Returns(new SalaryParseResult { SalaryMin = 100000, SalaryMax = 120000, SalaryPeriod = SalaryPeriod.Yearly });
            _jobProcessedRepoMock.Setup(r => r.UpsertByRawJobIdAsync(It.IsAny<JobProcessed>())).ReturnsAsync(true);

            var result = await _service.ProcessPendingJobsAsync(CancellationToken.None);

            Assert.Equal(1, result);
            _jobProcessedRepoMock.Verify(r => r.UpsertByRawJobIdAsync(It.IsAny<JobProcessed>()), Times.Once);
        }
    }
}
