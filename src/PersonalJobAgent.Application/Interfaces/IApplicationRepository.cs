using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Domain.Entities;
using PersonalJobAgent.Domain.Enums;
using JobApplication = PersonalJobAgent.Domain.Entities.Application;

namespace PersonalJobAgent.Application.Interfaces;

public interface IApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JobApplication?> GetByJobAndCandidateAsync(Guid jobId, Guid candidateId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<JobApplication> Applications, int TotalCount)> GetPageAsync(
        Guid candidateId,
        ApplicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<JobApplication>> GetPendingAsync(
        Guid candidateId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
    Task<ApplicationSummaryResponse> GetSummaryAsync(
        Guid candidateId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
    Task AddAsync(JobApplication application, CancellationToken cancellationToken = default);
    Task AddEventAsync(ApplicationEvent applicationEvent, CancellationToken cancellationToken = default);
}