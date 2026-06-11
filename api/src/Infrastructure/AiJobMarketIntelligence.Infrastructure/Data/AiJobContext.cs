using Microsoft.EntityFrameworkCore;
using AiJobMarketIntelligence.Domain.Entities;
using AiJobMarketIntelligence.Domain.Entities.UserPreferences;

namespace AiJobMarketIntelligence.Infrastructure.Data;

/// <summary>
/// Main database context for the AI Job Market Intelligence Platform.
/// Configures all entities, relationships, and constraints using Fluent API.
/// </summary>
public class AiJobContext : DbContext
{
    public AiJobContext(DbContextOptions<AiJobContext> options) : base(options)
    {
    }

    public DbSet<JobRaw> JobsRaw { get; set; }
    
    public DbSet<JobProcessed> JobsProcessed { get; set; }
    
    public DbSet<Skill> Skills { get; set; }
    
    public DbSet<JobSkill> JobSkills { get; set; }

    public DbSet<UserJobPreferences> UserJobPreferences { get; set; }

    public DbSet<JobApplication> JobApplications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure JobRaw entity
        modelBuilder.Entity<JobRaw>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Company)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(e => e.Location)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(e => e.Description)
                .IsRequired();

            entity.Property(e => e.SalaryRaw)
                .HasMaxLength(200);

            entity.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Url)
                .IsRequired()
                .HasMaxLength(2000);

            // MySQL note: a UNIQUE index on a utf8mb4(2000) column exceeds key length.
            // Enforce de-dupe at application-level and via a prefix unique index.
            entity.HasIndex(e => e.Url)
                .IsUnique()
                .HasDatabaseName("IX_JobsRaw_Url_Unique")
                .HasPrefixLength(191);

            entity.Property(e => e.PostedDate)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                // MySQL: use TIMESTAMP so DEFAULT CURRENT_TIMESTAMP is allowed under strict mode
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsProcessed)
                .HasDefaultValue(false);

            // Navigation to JobProcessed (1-to-1)
            entity.HasOne(e => e.JobProcessed)
                .WithOne(jp => jp.JobRaw)
                .HasForeignKey<JobProcessed>(jp => jp.JobRawId)
                .OnDelete(DeleteBehavior.Cascade);

            // Navigation to JobSkills (1-to-many)
            entity.HasMany(e => e.JobSkills)
                .WithOne(js => js.JobRaw)
                .HasForeignKey(js => js.JobRawId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure JobProcessed entity
        modelBuilder.Entity<JobProcessed>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.JobRawId)
                .IsRequired();

            entity.Property(e => e.SalaryMin);

            entity.Property(e => e.SalaryMax);

            entity.Property(e => e.Currency)
                .HasMaxLength(3);

            entity.Property(e => e.SalaryPeriod)
                .HasConversion<int>()
                .HasColumnType("int")
                .HasDefaultValue(SalaryPeriod.Unknown);

            entity.Property(e => e.ExperienceLevel)
                .HasMaxLength(50);

            entity.Property(e => e.ProcessedAt)
                .IsRequired()
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Foreign key configuration
            entity.HasOne(e => e.JobRaw)
                .WithOne(jr => jr.JobProcessed)
                .HasForeignKey<JobProcessed>(e => e.JobRawId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Skill entity
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            // Unique constraint on skill name
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_Skills_Name_Unique");

            // Navigation to JobSkills (1-to-many)
            entity.HasMany(e => e.JobSkills)
                .WithOne(js => js.Skill)
                .HasForeignKey(js => js.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure JobSkill join entity
        modelBuilder.Entity<JobSkill>(entity =>
        {
            entity.HasKey(js => new { js.JobRawId, js.SkillId });

            entity.Property(js => js.JobRawId)
                .IsRequired();

            entity.Property(js => js.SkillId)
                .IsRequired();

            // Foreign key to JobRaw
            entity.HasOne(js => js.JobRaw)
                .WithMany(jr => jr.JobSkills)
                .HasForeignKey(js => js.JobRawId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Skill
            entity.HasOne(js => js.Skill)
                .WithMany(s => s.JobSkills)
                .HasForeignKey(js => js.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            // Provider-safe check constraint (MySQL-compatible identifiers)
            entity.ToTable("JobSkills", t =>
                t.HasCheckConstraint("CK_JobSkill_Ids", "JobRawId > 0 AND SkillId > 0"));
        });

        // Configure UserJobPreferences entity
        modelBuilder.Entity<UserJobPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasDatabaseName("IX_UserJobPreferences_UserId_Unique");

            entity.Property(e => e.Location)
                .HasMaxLength(300);

            entity.Property(e => e.PreferredJobTitle)
                .HasMaxLength(200);

            entity.Property(e => e.WorkMode)
                .HasMaxLength(50);

            // Free-text skills
            entity.Property(e => e.SkillsText)
                .HasMaxLength(4000);

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure JobApplication entity
        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.JobRawId)
                .IsRequired();

            // Prevent duplicate applications per user/job
            entity.HasIndex(e => new { e.UserId, e.JobRawId })
                .IsUnique()
                .HasDatabaseName("IX_JobApplications_UserId_JobRawId_Unique");

            entity.Property(e => e.AppliedAt)
                .IsRequired()
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.JobRaw)
                .WithMany()
                .HasForeignKey(e => e.JobRawId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
