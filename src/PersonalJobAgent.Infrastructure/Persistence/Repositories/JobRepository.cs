using Microsoft.EntityFrameworkCore;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Repositories;

internal sealed class JobRepository : IJobRepository
{
    private readonly PersonalJobAgentDbContext _dbContext;

    public JobRepository(PersonalJobAgentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyCollection<Job> Jobs, int TotalCount)> GetPageAsync(
        string? search,
        string? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Jobs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(job =>
                job.Title.Contains(search) ||
                job.Company.Contains(search) ||
                job.Location.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(job => job.Source == source);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var jobs = await query
            .OrderByDescending(job => job.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (jobs, totalCount);
    }

    public async Task<Job?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.ExternalId == externalId, cancellationToken);
    }

    public async Task<Job?> GetByContentHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.ContentHash == contentHash, cancellationToken);
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        await _dbContext.Jobs.AddAsync(job, cancellationToken);
    }

    public Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        _dbContext.Jobs.Update(job);
        return Task.CompletedTask;
    }
}
