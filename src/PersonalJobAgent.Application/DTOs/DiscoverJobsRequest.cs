using PersonalJobAgent.Application.JobDiscovery.Models;

namespace PersonalJobAgent.Application.DTOs;

public sealed class DiscoverJobsRequest
{
    public IReadOnlyCollection<DiscoveredJob> Jobs { get; set; } = [];
}