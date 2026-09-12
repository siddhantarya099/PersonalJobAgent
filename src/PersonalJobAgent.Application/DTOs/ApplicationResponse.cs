using PersonalJobAgent.Domain.Enums;

namespace PersonalJobAgent.Application.DTOs;

public sealed class ApplicationResponse
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid CandidateId { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime? AppliedAtUtc { get; set; }

    public string ApplicationUrl { get; set; } = string.Empty;
    public Guid? ResumeVersionId { get; set; }

    public string NextAction { get; set; } = string.Empty;

    public DateTime LastStatusChangeUtc { get; set; }

    public IReadOnlyCollection<ApplicationEventResponse> Events { get; set; } = [];
}

public sealed class ApplicationEventResponse
{
    public ApplicationStatus OldStatus { get; set; }

    public ApplicationStatus NewStatus { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }
}