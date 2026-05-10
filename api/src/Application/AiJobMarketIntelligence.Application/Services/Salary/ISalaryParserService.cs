namespace AiJobMarketIntelligence.Application.Services.Salary;

public interface ISalaryParserService
{
    SalaryParseResult Parse(string? salaryRaw, string? description);
}
