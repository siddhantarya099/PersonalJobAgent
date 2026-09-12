using PersonalJobAgent.Application.JobDiscovery.Models;

namespace PersonalJobAgent.Application.Interfaces;

public interface IJobSource
{
    Task<IReadOnlyCollection<DiscoveredJob>> FetchJobsAsync(
        CancellationToken cancellationToken = default);
}