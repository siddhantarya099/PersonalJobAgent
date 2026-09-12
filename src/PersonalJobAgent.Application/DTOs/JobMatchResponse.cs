using PersonalJobAgent.Application.JobMatching.Models;

namespace PersonalJobAgent.Application.DTOs;

public sealed class JobMatchResponse
{
    public Guid JobMatchId { get; set; }

    public Guid JobId { get; set; }

    public Guid CandidateId { get; set; }

    public JobMatchResult MatchResult { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
}
