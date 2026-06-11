using System;

namespace AiJobMarketIntelligence.Domain.Entities
{
    /// <summary>
    /// Represents a job application created by a user for a specific raw job posting.
    /// Used for persistent session tracking of applied jobs.
    /// </summary>
    public class JobApplication
    {
        public int Id { get; set; }

        /// <summary>
        /// ASP.NET Core Identity user ID (string matches key type in AuthDbContext).
        /// </summary>
        public required string UserId { get; set; }

        public int JobRawId { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public JobRaw? JobRaw { get; set; }
    }
}
