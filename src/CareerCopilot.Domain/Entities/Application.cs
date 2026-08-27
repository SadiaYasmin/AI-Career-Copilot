using CareerCopilot.Domain.Enums;
using CareerCopilot.Domain.Exceptions;

namespace CareerCopilot.Domain.Entities;

public sealed class Application : AuditableEntity
{
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> AllowedTransitions = new()
    {
        [ApplicationStatus.Saved] = new[]
        {
            ApplicationStatus.Applied, ApplicationStatus.Screening, ApplicationStatus.Interview,
            ApplicationStatus.TechnicalRound, ApplicationStatus.FinalRound, ApplicationStatus.Offer,
            ApplicationStatus.Rejected, ApplicationStatus.Withdrawn
        },
        [ApplicationStatus.Applied] = new[]
        {
            ApplicationStatus.Screening, ApplicationStatus.Interview, ApplicationStatus.TechnicalRound,
            ApplicationStatus.FinalRound, ApplicationStatus.Offer, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn
        },
        [ApplicationStatus.Screening] = new[]
        {
            ApplicationStatus.Interview, ApplicationStatus.TechnicalRound, ApplicationStatus.FinalRound,
            ApplicationStatus.Offer, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn
        },
        [ApplicationStatus.Interview] = new[]
        {
            ApplicationStatus.TechnicalRound, ApplicationStatus.FinalRound, ApplicationStatus.Offer,
            ApplicationStatus.Rejected, ApplicationStatus.Withdrawn
        },
        [ApplicationStatus.TechnicalRound] = new[]
        {
            ApplicationStatus.FinalRound, ApplicationStatus.Offer, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn
        },
        [ApplicationStatus.FinalRound] = new[]
        {
            ApplicationStatus.Offer, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn
        },
        [ApplicationStatus.Offer] = new[]
        {
            ApplicationStatus.Rejected, ApplicationStatus.Withdrawn
        },
        [ApplicationStatus.Rejected] = Array.Empty<ApplicationStatus>(),
        [ApplicationStatus.Withdrawn] = Array.Empty<ApplicationStatus>()
    };

    public Guid UserId { get; set; }
    public Guid? JobId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string JobUrl { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? JobDescription { get; set; }
    public string Source { get; set; } = string.Empty;
    public Guid? ResumeId { get; set; }
    public Guid? CoverLetterId { get; set; }
    public int? MatchScore { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Saved;
    public DateTime? AppliedAt { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public string? Notes { get; set; }

    public User? User { get; set; }
    public Job? Job { get; set; }

    public void UpdateStatus(ApplicationStatus newStatus, DateTime utcNow)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!AllowedTransitions[Status].Contains(newStatus))
        {
            throw new DomainRuleException(
                $"Invalid application status transition from '{Status}' to '{newStatus}'.");
        }

        Status = newStatus;
        if (newStatus == ApplicationStatus.Applied && AppliedAt is null)
        {
            AppliedAt = utcNow;
        }

        UpdatedAt = utcNow;
    }
}