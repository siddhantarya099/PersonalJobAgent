using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Interfaces;

public interface IJobAnalysisService
{
    Task<JobMatch> AnalyzeJobAsync(
        Job job,
        JobProfile jobProfile,
        CancellationToken cancellationToken = default);
}
