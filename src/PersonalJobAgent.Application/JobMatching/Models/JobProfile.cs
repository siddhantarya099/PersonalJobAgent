namespace PersonalJobAgent.Application.JobMatching.Models;

public sealed record JobProfile
{
    public string Title { get; init; } = string.Empty;

    public string Company { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public decimal? SalaryMinLpa { get; init; }

    public decimal? SalaryMaxLpa { get; init; }

    public decimal? MinimumExperienceYears { get; init; }

    public decimal? MaximumExperienceYears { get; init; }

    public IReadOnlyCollection<string> RequiredSkills { get; init; } = [];

    public IReadOnlyCollection<string> PreferredSkills { get; init; } = [];

    public IReadOnlyCollection<string> MandatorySkills { get; init; } = [];

    public bool RemoteAvailable { get; init; }

    public bool RelocationAvailable { get; init; }

    public string Description { get; init; } = string.Empty;
}