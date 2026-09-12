using PersonalJobAgent.Domain.Common;
using PersonalJobAgent.Domain.Enums;

namespace PersonalJobAgent.Domain.Entities;

public sealed class JobMatch : BaseEntity
{
    public Guid JobId { get; private set; }

    public Guid CandidateId { get; private set; }

    public decimal OverallScore { get; private set; }

    public decimal TechnicalScore { get; private set; }

    public decimal ExperienceScore { get; private set; }

    public decimal SalaryScore { get; private set; }

    public decimal CompanyScore { get; private set; }

    public decimal LocationScore { get; private set; }

    public Recommendation Recommendation { get; private set; }

    public string Reasoning { get; private set; } = string.Empty;

    public string MatchedSkillsJson { get; private set; } = "[]";

    public string MissingSkillsJson { get; private set; } = "[]";

    public string MandatoryGapsJson { get; private set; } = "[]";

    public string ResumeStrategyJson { get; private set; } = "[]";

    public string ModelVersion { get; private set; } = string.Empty;

    private JobMatch()
    {
    }

    public JobMatch(
        Guid jobId,
        Guid candidateId,
        decimal overallScore,
        decimal technicalScore,
        decimal experienceScore,
        decimal salaryScore,
        decimal companyScore,
        decimal locationScore,
        Recommendation recommendation,
        string reasoning,
        string matchedSkillsJson,
        string missingSkillsJson,
        string mandatoryGapsJson,
        string resumeStrategyJson,
        string modelVersion)
    {
        JobId = jobId;
        CandidateId = candidateId;
        OverallScore = overallScore;
        TechnicalScore = technicalScore;
        ExperienceScore = experienceScore;
        SalaryScore = salaryScore;
        CompanyScore = companyScore;
        LocationScore = locationScore;
        Recommendation = recommendation;
        Reasoning = reasoning;
        MatchedSkillsJson = matchedSkillsJson;
        MissingSkillsJson = missingSkillsJson;
        MandatoryGapsJson = mandatoryGapsJson;
        ResumeStrategyJson = resumeStrategyJson;
        ModelVersion = modelVersion;
    }
}