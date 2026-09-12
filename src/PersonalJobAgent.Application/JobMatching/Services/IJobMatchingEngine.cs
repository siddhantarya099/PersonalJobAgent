using PersonalJobAgent.Application.JobMatching.Models;

namespace PersonalJobAgent.Application.JobMatching.Services;

public interface IJobMatchingEngine
{
    JobMatchResult Evaluate(
        CandidateProfile candidate,
        JobProfile job);
}