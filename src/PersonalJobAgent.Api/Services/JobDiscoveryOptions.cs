namespace PersonalJobAgent.Api.Services;

public sealed class JobDiscoveryOptions
{
    public const string SectionName = "JobDiscovery";

    public bool Enabled { get; set; }

    public string FeedUrl { get; set; } = string.Empty;

    public int IntervalMinutes { get; set; } = 60;
}