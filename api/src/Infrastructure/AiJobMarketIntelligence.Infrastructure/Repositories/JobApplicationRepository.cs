using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiJobMarketIntelligence.Application.Interfaces.Repositories;
using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiJobMarketIntelligence.Infrastructure.Repositories
{
    public class JobApplicationRepository : IJobApplicationRepository
    {
        private readonly AiJobContext _context;

        public JobApplicationRepository(AiJobContext context)
        {
            _context = context;
        }

        public async Task<List<JobRaw>> GetAppliedJobsByUserIdAsync(string userId)
        {
            return await _context.JobApplications
                .Where(a => a.UserId == userId)
                .Include(a => a.JobRaw)
                    .ThenInclude(j => j!.JobSkills)
                        .ThenInclude(js => js.Skill)
                .Include(a => a.JobRaw)
                    .ThenInclude(j => j!.JobProcessed)
                .Select(a => a.JobRaw!)
                .ToListAsync();
        }

        public async Task<bool> ApplyJobAsync(string userId, int jobId)
        {
            var exists = await _context.JobApplications.AnyAsync(a => a.UserId == userId && a.JobRawId == jobId);
            if (exists) return true;

            var jobExists = await _context.JobsRaw.AnyAsync(j => j.Id == jobId);
            if (!jobExists) return false;

            var app = new JobApplication { UserId = userId, JobRawId = jobId };
            _context.JobApplications.Add(app);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnapplyJobAsync(string userId, int jobId)
        {
            var app = await _context.JobApplications.FirstOrDefaultAsync(a => a.UserId == userId && a.JobRawId == jobId);
            if (app is null) return false;

            _context.JobApplications.Remove(app);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsAppliedAsync(string userId, int jobId)
        {
            return await _context.JobApplications.AnyAsync(a => a.UserId == userId && a.JobRawId == jobId);
        }
    }
}
