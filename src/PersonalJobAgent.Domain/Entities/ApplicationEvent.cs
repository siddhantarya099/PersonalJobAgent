using PersonalJobAgent.Domain.Common;
using PersonalJobAgent.Domain.Enums;

namespace PersonalJobAgent.Domain.Entities;

public sealed class ApplicationEvent : BaseEntity
{
    public Guid ApplicationId { get; private set; }

    public ApplicationStatus OldStatus { get; private set; }

    public ApplicationStatus NewStatus { get; private set; }

    public string Source { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    private ApplicationEvent()
    {
    }

    public ApplicationEvent(
        Guid applicationId,
        ApplicationStatus oldStatus,
        ApplicationStatus newStatus,
        string source,
        string description)
    {
        ApplicationId = applicationId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Source = source;
        Description = description;
        OccurredAtUtc = DateTime.UtcNow;
    }
}
