using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Interfaces;

public interface IJobMatchRepository
{
    Task<JobMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JobMatch?> GetByJobAndCandidateAsync(Guid jobId, Guid candidateId, CancellationToken cancellationToken = default);
    Task AddAsync(JobMatch jobMatch, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobMatch jobMatch, CancellationToken cancellationToken = default);
}
