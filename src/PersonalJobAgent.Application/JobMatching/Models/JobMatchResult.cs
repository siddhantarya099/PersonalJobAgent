namespace PersonalJobAgent.Application.JobMatching.Models;

public sealed record JobMatchResult
{
    public decimal OverallScore { get; init; }

    public decimal TechnicalScore { get; init; }

    public decimal ExperienceScore { get; init; }

    public decimal SalaryScore { get; init; }

    public decimal LocationScore { get; init; }

    public IReadOnlyCollection<string> MatchedSkills { get; init; } = [];

    public IReadOnlyCollection<string> MissingSkills { get; init; } = [];

    public IReadOnlyCollection<string> MandatoryGaps { get; init; } = [];

    public bool HasHardBlocker { get; init; }

    public string Recommendation { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Reasons { get; init; } = [];
}