using PersonalJobAgent.Application.JobMatching.Models;

namespace PersonalJobAgent.Application.Interfaces;

public interface IJdParserService
{
    Task<JobProfile> ParseAsync(
        string jobDescription,
        CancellationToken cancellationToken = default);
}