using Microsoft.EntityFrameworkCore;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Domain.Entities;
using PersonalJobAgent.Domain.Enums;
using JobApplication = PersonalJobAgent.Domain.Entities.Application;

namespace PersonalJobAgent.Infrastructure.Persistence.Repositories;

internal sealed class ApplicationRepository : IApplicationRepository
{
    private readonly PersonalJobAgentDbContext _dbContext;

    public ApplicationRepository(PersonalJobAgentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Applications
            .Include(application => application.Events)
            .FirstOrDefaultAsync(application => application.Id == id, cancellationToken);
    }

    public async Task<JobApplication?> GetByJobAndCandidateAsync(Guid jobId, Guid candidateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Applications
            .Include(application => application.Events)
            .FirstOrDefaultAsync(application =>
                application.JobId == jobId && application.CandidateId == candidateId,
                cancellationToken);
    }

        public async Task<(IReadOnlyCollection<JobApplication> Applications, int TotalCount)> GetPageAsync(
            Guid candidateId,
            ApplicationStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Applications
                .Include(application => application.Events)
                .Where(application => application.CandidateId == candidateId);

            if (status.HasValue)
            {
                query = query.Where(application => application.Status == status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var applications = await query
                .OrderByDescending(application => application.LastStatusChangeUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (applications, totalCount);
        }

        public async Task<IReadOnlyCollection<JobApplication>> GetPendingAsync(
            Guid candidateId,
            DateTime asOfUtc,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Applications
                .Include(application => application.Events)
                .Where(application =>
                    application.CandidateId == candidateId &&
                    application.NextActionDateUtc.HasValue &&
                    application.NextActionDateUtc <= asOfUtc &&
                    application.Status != ApplicationStatus.Closed &&
                    application.Status != ApplicationStatus.Rejected)
                .OrderBy(application => application.NextActionDateUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<ApplicationSummaryResponse> GetSummaryAsync(
            Guid candidateId,
            DateTime asOfUtc,
            CancellationToken cancellationToken = default)
        {
            var applications = _dbContext.Applications
                .Where(application => application.CandidateId == candidateId);

            var counts = await applications
                .GroupBy(application => application.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            var pendingActions = await applications.CountAsync(application =>
                application.NextActionDateUtc.HasValue &&
                application.NextActionDateUtc <= asOfUtc &&
                application.Status != ApplicationStatus.Closed &&
                application.Status != ApplicationStatus.Rejected,
                cancellationToken);

            return new ApplicationSummaryResponse
            {
                Total = counts.Sum(item => item.Count),
                ByStatus = counts.ToDictionary(item => item.Status, item => item.Count),
                PendingActions = pendingActions
            };
        }

    public async Task AddAsync(JobApplication application, CancellationToken cancellationToken = default)
    {
        await _dbContext.Applications.AddAsync(application, cancellationToken);
    }

    public async Task AddEventAsync(ApplicationEvent applicationEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.ApplicationEvents.AddAsync(applicationEvent, cancellationToken);
    }
}