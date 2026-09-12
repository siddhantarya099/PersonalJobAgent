using PersonalJobAgent.Domain.Common;
using PersonalJobAgent.Domain.Enums;

namespace PersonalJobAgent.Domain.Entities;

public sealed class Application : BaseEntity
{
    public Guid CandidateId { get; private set; }

    public Guid JobId { get; private set; }

    public ApplicationStatus Status { get; private set; }

    public DateTime? AppliedAtUtc { get; private set; }

    public Guid? ResumeVersionId { get; private set; }

    public Guid? CoverLetterVersionId { get; private set; }

    public string ApplicationUrl { get; private set; } = string.Empty;

    public DateTime LastStatusChangeUtc { get; private set; }

    public string NextAction { get; private set; } = string.Empty;

    public DateTime? NextActionDateUtc { get; private set; }

    private readonly List<ApplicationEvent> _events = [];

    public IReadOnlyCollection<ApplicationEvent> Events => _events.AsReadOnly();

    private Application()
    {
    }

    public Application(
        Guid candidateId,
        Guid jobId,
        ApplicationStatus status,
        string applicationUrl)
    {
        CandidateId = candidateId;
        JobId = jobId;
        Status = status;
        ApplicationUrl = applicationUrl;
        LastStatusChangeUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(
        ApplicationStatus newStatus,
        string action = "",
        DateTime? nextActionDateUtc = null)
    {
        if (Status == newStatus)
        {
            return;
        }

        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Application status cannot change from {Status} to {newStatus}.");
        }

        Status = newStatus;
        LastStatusChangeUtc = DateTime.UtcNow;
        NextAction = action;
        NextActionDateUtc = nextActionDateUtc;

        if (newStatus == ApplicationStatus.Applied && !AppliedAtUtc.HasValue)
        {
            AppliedAtUtc = DateTime.UtcNow;
        }
    }

    public void RecordEvent(ApplicationEvent applicationEvent)
    {
        if (applicationEvent.ApplicationId != Id)
        {
            throw new ArgumentException("The event belongs to a different application.", nameof(applicationEvent));
        }

        _events.Add(applicationEvent);
    }

    public void AttachResumeVersion(Guid resumeVersionId)
    {
        ResumeVersionId = resumeVersionId;
    }

    private bool CanTransitionTo(ApplicationStatus newStatus)
    {
        return Status switch
        {
            ApplicationStatus.Discovered => newStatus is
                ApplicationStatus.Shortlisted or
                ApplicationStatus.ReadyToApply or
                ApplicationStatus.Applied or
                ApplicationStatus.Rejected or
                ApplicationStatus.Withdrawn or
                ApplicationStatus.Closed,
            ApplicationStatus.Shortlisted => newStatus is
                ApplicationStatus.ReadyToApply or
                ApplicationStatus.Applied or
                ApplicationStatus.Rejected or
                ApplicationStatus.Withdrawn or
                ApplicationStatus.Closed,
            ApplicationStatus.ReadyToApply => newStatus is
                ApplicationStatus.Applied or
                ApplicationStatus.Rejected or
                ApplicationStatus.Withdrawn or
                ApplicationStatus.Closed,
            ApplicationStatus.Applied => newStatus is
                ApplicationStatus.Assessment or
                ApplicationStatus.RecruiterContacted or
                ApplicationStatus.Interview or
                ApplicationStatus.Offer or
                ApplicationStatus.Rejected or
                ApplicationStatus.Withdrawn or
                ApplicationStatus.Closed,
            ApplicationStatus.Assessment => newStatus is
                ApplicationStatus.Interview or
                ApplicationStatus.Offer or
                ApplicationStatus.Rejected or
                ApplicationStatus.Withdrawn or
                ApplicationStatus.Closed,
            ApplicationStatus.RecruiterContacted => newStatus is
                ApplicationStatus.Interview or
                ApplicationStatus.Offer or
                ApplicationStatus.Rejected or
                ApplicationStatus.Withdrawn or
                ApplicationStatus.Closed,
            ApplicationStatus.Interview => newStatus is
                ApplicationStatus.Offer or
                ApplicationStatus.Rejected or
                ApplicationStatus.Withdrawn or
                ApplicationStatus.Closed,
            ApplicationStatus.Offer => newStatus is
                ApplicationStatus.Withdrawn or
                ApplicationStatus.Closed,
            _ => false
        };
    }
}
