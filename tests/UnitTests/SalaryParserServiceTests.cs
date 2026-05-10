using Xunit;
using AiJobMarketIntelligence.Application.Services.Salary;

namespace AiJobMarketIntelligence.UnitTests
{
    public class SalaryParserServiceTests
    {
        private readonly SalaryParserService _service = new SalaryParserService();

        [Theory]
        [InlineData("$100k - $120k per year", 100000, 120000, SalaryPeriod.Yearly)]
        [InlineData("£50k - £60k per annum", 50000, 60000, SalaryPeriod.Yearly)]
        [InlineData("€30k per month", 30000, 30000, SalaryPeriod.Monthly)]
        [InlineData("$20 per hour", 20, 20, SalaryPeriod.Hourly)]
        [InlineData("Not a salary", 0, 0, SalaryPeriod.Unknown)]
        public void ParseSalary_ValidInputs_ReturnsExpected(string input, int expectedMin, int expectedMax, SalaryPeriod expectedPeriod)
        {
            var result = _service.ParseSalary(input);
            Assert.Equal(expectedMin, result.SalaryMin);
            Assert.Equal(expectedMax, result.SalaryMax);
            Assert.Equal(expectedPeriod, result.SalaryPeriod);
        }
    }
}
