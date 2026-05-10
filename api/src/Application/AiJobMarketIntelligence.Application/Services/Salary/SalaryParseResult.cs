using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Application.Services.Salary;

public sealed class SalaryParseResult
{
    public int? SalaryMin { get; init; }
    public int? SalaryMax { get; init; }
    public string? Currency { get; init; }
    public SalaryPeriod SalaryPeriod { get; init; } = SalaryPeriod.Unknown;

    public bool HasSalary => SalaryMin != null || SalaryMax != null;
}
