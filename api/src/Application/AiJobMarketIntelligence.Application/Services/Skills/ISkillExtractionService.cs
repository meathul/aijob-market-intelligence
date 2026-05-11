using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiJobMarketIntelligence.Application.Services.Skills;

public interface ISkillExtractionService
{
    /// <summary>
    /// Extract skills from job description using AI/heuristics
    /// </summary>
    Task<List<string>> ExtractSkillsAsync(string jobDescription, string jobTitle);
}
