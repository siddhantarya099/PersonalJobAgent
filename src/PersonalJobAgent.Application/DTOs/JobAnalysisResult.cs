namespace PersonalJobAgent.Application.DTOs;

public sealed record JobAnalysisResult(
    decimal OverallScore,
    decimal TechnicalScore,
    decimal ExperienceScore,
    decimal SalaryScore,
    decimal CompanyScore,
    decimal LocationScore,
    string Recommendation,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> MandatoryGaps,
    IReadOnlyList<string> ResumeStrategy,
    string Reasoning);