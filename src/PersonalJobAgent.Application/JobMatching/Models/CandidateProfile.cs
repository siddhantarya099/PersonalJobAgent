namespace PersonalJobAgent.Application.JobMatching.Models;

public sealed record CandidateProfile
{
    public decimal ExperienceYears { get; init; }

    public decimal MinimumSalaryLpa { get; init; }

    public decimal TargetSalaryLpa { get; init; }

    public string CurrentLocation { get; init; } = string.Empty;

    public bool OpenToRemote { get; init; }

    public bool OpenToRelocation { get; init; }

    public int NoticePeriodMonths { get; init; }

    public IReadOnlyCollection<string> Skills { get; init; } = [];
}