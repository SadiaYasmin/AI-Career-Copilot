using CareerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Infrastructure.Persistence;

internal static class ModelConfiguration
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Profile).WithOne(p => p.User).HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Education>(e => e.HasOne(x => x.UserProfile).WithMany(p => p.Education)
            .HasForeignKey(x => x.UserProfileId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<Experience>(e => e.HasOne(x => x.UserProfile).WithMany(p => p.Experiences)
            .HasForeignKey(x => x.UserProfileId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<Project>(e => e.HasOne(x => x.UserProfile).WithMany(p => p.Projects)
            .HasForeignKey(x => x.UserProfileId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<Skill>(e => e.HasOne(x => x.UserProfile).WithMany(p => p.Skills)
            .HasForeignKey(x => x.UserProfileId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<Certification>(e => e.HasOne(x => x.UserProfile).WithMany(p => p.Certifications)
            .HasForeignKey(x => x.UserProfileId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<CareerGoal>(e => e.HasOne(x => x.UserProfile).WithMany(p => p.Goals)
            .HasForeignKey(x => x.UserProfileId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<LinkedInProfile>(e => e.HasOne(x => x.UserProfile).WithOne(p => p.LinkedInProfile)
            .HasForeignKey<LinkedInProfile>(x => x.UserProfileId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<Resume>(e => e.HasOne(x => x.User).WithMany(u => u.Resumes)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<TailoredResume>(e =>
        {
            e.HasOne(x => x.Resume).WithMany(r => r.TailoredResumes)
                .HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<CoverLetter>(e =>
        {
            e.HasOne(x => x.Job).WithMany(j => j.CoverLetters).HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Job>(e => e.HasOne(x => x.User).WithMany(u => u.Jobs)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<JobRequirement>(e => e.HasOne(x => x.Job).WithMany(j => j.Requirements)
            .HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<JobMatch>(e =>
        {
            e.HasOne(x => x.Job).WithMany(j => j.Matches).HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Resume).WithMany().HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<SkillGap>(e =>
        {
            e.HasOne(x => x.Job).WithMany(j => j.SkillGaps).HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.SkillGaps).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ApplicationEntity>(e =>
        {
            e.HasOne(x => x.Job).WithMany(j => j.Applications).HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Applications).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<InterviewSession>(e =>
        {
            e.HasOne(x => x.Job).WithMany(j => j.InterviewSessions).HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.InterviewSessions).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<InterviewQuestion>(e => e.HasOne(x => x.InterviewSession).WithMany(s => s.Questions)
            .HasForeignKey(x => x.InterviewSessionId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<InterviewAnswer>(e => e.HasOne(x => x.InterviewQuestion).WithMany(q => q.Answers)
            .HasForeignKey(x => x.InterviewQuestionId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<InterviewEvaluation>(e => e.HasOne(x => x.InterviewAnswer).WithOne(a => a.Evaluation)
            .HasForeignKey<InterviewEvaluation>(x => x.InterviewAnswerId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<CareerRoadmap>(e => e.HasOne(x => x.User).WithMany(u => u.CareerRoadmaps)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<RoadmapTask>(e => e.HasOne(x => x.CareerRoadmap).WithMany(r => r.Tasks)
            .HasForeignKey(x => x.CareerRoadmapId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<Reminder>(e =>
        {
            e.HasOne(x => x.User).WithMany(u => u.Reminders).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<CopilotConversation>(e =>
        {
            e.HasOne(x => x.User).WithMany(u => u.CopilotConversations).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Resume>().WithMany().HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<CopilotMessage>(e => e.HasOne(x => x.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<RecruiterReadinessScore>(e => e.HasOne(x => x.User).WithMany(u => u.RecruiterReadinessScores)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade));
    }
}