using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Interfaces;

public interface IJobDescriptionEnricher
{
    bool CanEnrich(Job job);

    Task<string?> EnrichAsync(
        Job job,
        CancellationToken cancellationToken = default);
}