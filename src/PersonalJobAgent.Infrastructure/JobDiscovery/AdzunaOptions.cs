namespace PersonalJobAgent.Infrastructure.JobDiscovery;

public sealed class AdzunaOptions
{
    public const string SectionName = "Adzuna";

    public string AppId { get; set; } = string.Empty;

    public string AppKey { get; set; } = string.Empty;

    public string Country { get; set; } = "in";

    public int ResultsPerPage { get; set; } = 20;

    public int MaxPages { get; set; } = 2;

    public List<string> Keywords { get; set; } = [];

    public List<string> Locations { get; set; } = [];
}