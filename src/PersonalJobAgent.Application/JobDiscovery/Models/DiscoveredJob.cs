namespace PersonalJobAgent.Application.JobDiscovery.Models;

public sealed record DiscoveredJob
{
    public string ExternalId { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Company { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public decimal? SalaryMinLpa { get; init; }

    public decimal? SalaryMaxLpa { get; init; }

    public string JobUrl { get; init; } = string.Empty;

    public DateTime? PostedAtUtc { get; init; }
}