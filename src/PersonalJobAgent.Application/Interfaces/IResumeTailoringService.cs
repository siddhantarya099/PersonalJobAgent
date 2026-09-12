using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Interfaces;

public interface IResumeTailoringService
{
    Task<ResumeVersion> CreateTailoredVersionAsync(
        Resume resume,
        JobProfile jobProfile,
        JobMatchResult matchResult,
        CancellationToken cancellationToken = default);
}