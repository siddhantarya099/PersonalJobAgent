using Microsoft.EntityFrameworkCore;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Repositories;

internal sealed class ResumeRepository : IResumeRepository
{
    private readonly PersonalJobAgentDbContext _dbContext;

    public ResumeRepository(PersonalJobAgentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Resume?> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Resumes
            .Include(resume => resume.Versions.OrderByDescending(version => version.VersionNumber))
            .FirstOrDefaultAsync(resume => resume.CandidateId == candidateId, cancellationToken);
    }

    public async Task<ResumeVersion?> GetVersionByIdAsync(Guid resumeVersionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResumeVersions
            .Include(version => version.Resume)
            .FirstOrDefaultAsync(version => version.Id == resumeVersionId, cancellationToken);
    }

    public async Task AddAsync(Resume resume, CancellationToken cancellationToken = default)
    {
        await _dbContext.Resumes.AddAsync(resume, cancellationToken);
    }

    public async Task AddVersionAsync(ResumeVersion version, CancellationToken cancellationToken = default)
    {
        await _dbContext.ResumeVersions.AddAsync(version, cancellationToken);
    }
}