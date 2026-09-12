using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Interfaces;

public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Candidate?> GetActiveCandidateAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default);
    Task UpdateAsync(Candidate candidate, CancellationToken cancellationToken = default);
}
