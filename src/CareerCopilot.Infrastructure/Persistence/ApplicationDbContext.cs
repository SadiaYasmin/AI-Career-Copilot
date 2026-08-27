using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Education> Education => Set<Education>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<CareerGoal> CareerGoals => Set<CareerGoal>();
    public DbSet<LinkedInProfile> LinkedInProfiles => Set<LinkedInProfile>();

    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<TailoredResume> TailoredResumes => Set<TailoredResume>();
    public DbSet<CoverLetter> CoverLetters => Set<CoverLetter>();

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobRequirement> JobRequirements => Set<JobRequirement>();
    public DbSet<JobMatch> JobMatches => Set<JobMatch>();
    public DbSet<SkillGap> SkillGaps => Set<SkillGap>();

    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();

    public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
    public DbSet<InterviewAnswer> InterviewAnswers => Set<InterviewAnswer>();
    public DbSet<InterviewEvaluation> InterviewEvaluations => Set<InterviewEvaluation>();

    public DbSet<CareerRoadmap> CareerRoadmaps => Set<CareerRoadmap>();
    public DbSet<RoadmapTask> RoadmapTasks => Set<RoadmapTask>();

    public DbSet<Reminder> Reminders => Set<Reminder>();

    public DbSet<CopilotConversation> CopilotConversations => Set<CopilotConversation>();
    public DbSet<CopilotMessage> CopilotMessages => Set<CopilotMessage>();

    public DbSet<RecruiterReadinessScore> RecruiterReadinessScores => Set<RecruiterReadinessScore>();

    IQueryable<T> IApplicationDbContext.Set<T>() where T : class => Set<T>();

    void IApplicationDbContext.Add<T>(T entity) where T : class => Add(entity);

    void IApplicationDbContext.AddRange<T>(IEnumerable<T> entities) where T : class => AddRange(entities);

    void IApplicationDbContext.Update<T>(T entity) where T : class => Update(entity);

    void IApplicationDbContext.Remove<T>(T entity) where T : class => Remove(entity);

    void IApplicationDbContext.RemoveRange<T>(IEnumerable<T> entities) where T : class => RemoveRange(entities);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<CareerCopilot.Domain.Common.AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = now;
                    }

                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ModelConfiguration.Configure(modelBuilder);
    }
}