using System.Text.Json;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Application.JobMatching.Services;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Services;

public sealed class JobAnalysisService : IJobAnalysisService
{
    private readonly IJobMatchingEngine _matchingEngine;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IJobMatchRepository _jobMatchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JobAnalysisService(
        IJobMatchingEngine matchingEngine,
        ICandidateRepository candidateRepository,
        IJobMatchRepository jobMatchRepository,
        IUnitOfWork unitOfWork)
    {
        _matchingEngine = matchingEngine;
        _candidateRepository = candidateRepository;
        _jobMatchRepository = jobMatchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<JobMatch> AnalyzeJobAsync(
        Job job,
        JobProfile jobProfile,
        CancellationToken cancellationToken = default)
    {
        // Get the active candidate
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            throw new InvalidOperationException("No active candidate found in the database.");
        }

        // Convert candidate to profile
        var candidateProfile = new CandidateProfile
        {
            ExperienceYears = candidate.TotalExperienceYears,
            MinimumSalaryLpa = candidate.MinimumSalaryLpa,
            TargetSalaryLpa = candidate.TargetSalaryLpa,
            CurrentLocation = candidate.CurrentLocation,
            OpenToRemote = true, // Assumption; can be made configurable
            OpenToRelocation = true, // Assumption; can be made configurable
            NoticePeriodMonths = candidate.NoticePeriodMonths,
            Skills = candidate.Skills.Select(s => s.Name).ToList()
        };

        // Run the matching engine
        var matchResult = _matchingEngine.Evaluate(candidateProfile, jobProfile);

        // Convert result to JSON for storage
        var matchedSkillsJson = JsonSerializer.Serialize(matchResult.MatchedSkills);
        var missingSkillsJson = JsonSerializer.Serialize(matchResult.MissingSkills);
        var mandatoryGapsJson = JsonSerializer.Serialize(matchResult.MandatoryGaps);
        var reasonsJson = JsonSerializer.Serialize(matchResult.Reasons);

        // Create JobMatch entity
        var jobMatch = new JobMatch(
            jobId: job.Id,
            candidateId: candidate.Id,
            overallScore: matchResult.OverallScore,
            technicalScore: matchResult.TechnicalScore,
            experienceScore: matchResult.ExperienceScore,
            salaryScore: matchResult.SalaryScore,
            companyScore: 0m, // Placeholder; can be enhanced
            locationScore: matchResult.LocationScore,
            recommendation: ConvertRecommendation(matchResult.Recommendation),
            reasoning: string.Join("; ", matchResult.Reasons),
            matchedSkillsJson: matchedSkillsJson,
            missingSkillsJson: missingSkillsJson,
            mandatoryGapsJson: mandatoryGapsJson,
            resumeStrategyJson: reasonsJson,
            modelVersion: "1.0");

        // Persist the match
        await _jobMatchRepository.AddAsync(jobMatch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return jobMatch;
    }

    private Domain.Enums.Recommendation ConvertRecommendation(string recommendation) =>
        recommendation switch
        {
            "STRONGLY_RECOMMENDED" => Domain.Enums.Recommendation.StronglyRecommended,
            "RECOMMENDED" => Domain.Enums.Recommendation.Recommended,
            "POTENTIAL" => Domain.Enums.Recommendation.Potential,
            "LOW_PRIORITY" => Domain.Enums.Recommendation.LowPriority,
            _ => Domain.Enums.Recommendation.LowPriority
        };
}
