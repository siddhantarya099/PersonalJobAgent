using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Interfaces;

public interface IResumeRepository
{
    Task<Resume?> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<ResumeVersion?> GetVersionByIdAsync(Guid resumeVersionId, CancellationToken cancellationToken = default);
    Task AddAsync(Resume resume, CancellationToken cancellationToken = default);
    Task AddVersionAsync(ResumeVersion version, CancellationToken cancellationToken = default);
}