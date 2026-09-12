using PersonalJobAgent.Application.Interfaces;

namespace PersonalJobAgent.Infrastructure.Persistence.Repositories;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly PersonalJobAgentDbContext _dbContext;

    public UnitOfWork(PersonalJobAgentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
