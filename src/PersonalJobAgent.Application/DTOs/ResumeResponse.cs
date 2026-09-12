namespace PersonalJobAgent.Application.DTOs;

public sealed class ResumeResponse
{
    public Guid Id { get; set; }

    public Guid CandidateId { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyCollection<ResumeVersionResponse> Versions { get; set; } = [];
}

public sealed class ResumeVersionResponse
{
    public Guid Id { get; set; }

    public int VersionNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}