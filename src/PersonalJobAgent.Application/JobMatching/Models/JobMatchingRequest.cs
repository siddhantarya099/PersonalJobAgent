namespace PersonalJobAgent.Application.JobMatching.Models;

public sealed record JobMatchingRequest
{
    public CandidateProfile Candidate { get; init; } = new();

    public JobProfile Job { get; init; } = new();
}