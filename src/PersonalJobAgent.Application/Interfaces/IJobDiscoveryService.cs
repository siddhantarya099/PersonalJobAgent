using PersonalJobAgent.Application.JobDiscovery.Models;

namespace PersonalJobAgent.Application.Interfaces;

public interface IJobDiscoveryService
{
    Task<IReadOnlyCollection<Guid>> ImportAsync(
        IJobSource source,
        CancellationToken cancellationToken = default);
}