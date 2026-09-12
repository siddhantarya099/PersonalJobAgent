using Microsoft.EntityFrameworkCore;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Repositories;

internal sealed class JobMatchRepository : IJobMatchRepository
{
    private readonly PersonalJobAgentDbContext _dbContext;

    public JobMatchRepository(PersonalJobAgentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JobMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JobMatches
            .FirstOrDefaultAsync(jm => jm.Id == id, cancellationToken);
    }

    public async Task<JobMatch?> GetByJobAndCandidateAsync(Guid jobId, Guid candidateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JobMatches
            .FirstOrDefaultAsync(jm => jm.JobId == jobId && jm.CandidateId == candidateId, cancellationToken);
    }

    public async Task AddAsync(JobMatch jobMatch, CancellationToken cancellationToken = default)
    {
        await _dbContext.JobMatches.AddAsync(jobMatch, cancellationToken);
    }

    public Task UpdateAsync(JobMatch jobMatch, CancellationToken cancellationToken = default)
    {
        _dbContext.JobMatches.Update(jobMatch);
        return Task.CompletedTask;
    }
}
