using Xunit;
using AiJobMarketIntelligence.Application.Services.Salary;
using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.UnitTests
{
    public class SalaryParserServiceTests
    {
        private readonly SalaryParserService _service = new SalaryParserService();

        [Theory]
        [InlineData("$100k - $120k per year", null)]
        [InlineData("£50k - £60k per annum", null)]
        [InlineData("€30k per month", null)]
        [InlineData(null, "$20 per hour")]
        public void Parse_ValidInputs_ReturnsExpected(string? salaryRaw, string? description)
        {
            var result = _service.Parse(salaryRaw, description);
            Assert.NotNull(result);
            Assert.True(result.SalaryMin >= 0 || result.SalaryMax >= 0);
        }
    }
}
