using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<Job> Jobs, int TotalCount)> GetPageAsync(
        string? search,
        string? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Job?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<Job?> GetByContentHashAsync(string contentHash, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    Task UpdateAsync(Job job, CancellationToken cancellationToken = default);
}
