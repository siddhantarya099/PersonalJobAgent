using Microsoft.EntityFrameworkCore;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Repositories;

internal sealed class CandidateRepository : ICandidateRepository
{
    private readonly PersonalJobAgentDbContext _dbContext;

    public CandidateRepository(PersonalJobAgentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Candidates
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Candidate?> GetActiveCandidateAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Candidates
            .Include(c => c.Skills)
            .Where(c => c.IsActivelyLooking)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default)
    {
        await _dbContext.Candidates.AddAsync(candidate, cancellationToken);
    }

    public Task UpdateAsync(Candidate candidate, CancellationToken cancellationToken = default)
    {
        _dbContext.Candidates.Update(candidate);
        return Task.CompletedTask;
    }
}
