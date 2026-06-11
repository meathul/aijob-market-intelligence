using System.Collections.Generic;
using System.Threading.Tasks;
using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Application.Interfaces.Repositories
{
    public interface IJobApplicationRepository
    {
        Task<List<JobRaw>> GetAppliedJobsByUserIdAsync(string userId);
        Task<bool> ApplyJobAsync(string userId, int jobId);
        Task<bool> UnapplyJobAsync(string userId, int jobId);
        Task<bool> IsAppliedAsync(string userId, int jobId);
    }
}
