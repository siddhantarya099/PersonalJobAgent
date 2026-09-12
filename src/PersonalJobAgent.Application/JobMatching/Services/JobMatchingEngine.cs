using PersonalJobAgent.Application.JobMatching.Models;

namespace PersonalJobAgent.Application.JobMatching.Services;

public sealed class JobMatchingEngine : IJobMatchingEngine
{
    private readonly ISkillCanonicalizer _skillCanonicalizer;

    public JobMatchingEngine() : this(new SkillCanonicalizer())
    {
    }

    public JobMatchingEngine(ISkillCanonicalizer skillCanonicalizer)
    {
        _skillCanonicalizer = skillCanonicalizer;
    }

    public JobMatchResult Evaluate(
        CandidateProfile candidate,
        JobProfile job)
    {
        var candidateSkillKeys = _skillCanonicalizer.GetCanonicalKeys(candidate.Skills);

        var matchedSkills = job.RequiredSkills
            .Where(skill =>
                candidateSkillKeys.Contains(_skillCanonicalizer.GetCanonicalKey(skill)))
            .ToList();

        var missingSkills = job.RequiredSkills
            .Where(skill =>
                !candidateSkillKeys.Contains(_skillCanonicalizer.GetCanonicalKey(skill)))
            .ToList();

        var mandatoryGaps = job.MandatorySkills
            .Where(skill =>
                !candidateSkillKeys.Contains(_skillCanonicalizer.GetCanonicalKey(skill)))
            .ToList();

        var technicalScore = CalculateTechnicalScore(
            candidate,
            job,
            candidateSkillKeys);

        var experienceScore = CalculateExperienceScore(
            candidate,
            job);

        var salaryScore = CalculateSalaryScore(
            candidate,
            job);

        var locationScore = CalculateLocationScore(
            candidate,
            job);

        var hasHardBlocker =
            mandatoryGaps.Count > 0 ||
            IsExperienceHardBlocker(candidate, job) ||
            IsLocationHardBlocker(candidate, job);

        var overallScore = CalculateOverallScore(
            technicalScore,
            experienceScore,
            salaryScore,
            locationScore);

        if (hasHardBlocker)
        {
            overallScore = Math.Min(overallScore, 59);
        }

        var recommendation = GetRecommendation(
            overallScore,
            hasHardBlocker);

        return new JobMatchResult
        {
            OverallScore = Math.Round(overallScore, 2),
            TechnicalScore = technicalScore,
            ExperienceScore = experienceScore,
            SalaryScore = salaryScore,
            LocationScore = locationScore,

            MatchedSkills = matchedSkills,
            MissingSkills = missingSkills,
            MandatoryGaps = mandatoryGaps,

            HasHardBlocker = hasHardBlocker,

            Recommendation = recommendation,

            Reasons = BuildReasons(
                candidate,
                job,
                matchedSkills,
                missingSkills,
                mandatoryGaps,
                experienceScore,
                salaryScore)
        };
    }

    private decimal CalculateTechnicalScore(
        CandidateProfile candidate,
        JobProfile job,
        IReadOnlySet<string>? candidateSkillKeys = null)
    {
        if (job.RequiredSkills.Count == 0)
        {
            return 50;
        }

        var candidateSkills = candidateSkillKeys ?? _skillCanonicalizer.GetCanonicalKeys(candidate.Skills);

        var requiredMatches = job.RequiredSkills
            .Count(skill =>
                candidateSkills.Contains(_skillCanonicalizer.GetCanonicalKey(skill)));

        var requiredScore =
            (decimal)requiredMatches /
            job.RequiredSkills.Count *
            100;

        if (job.PreferredSkills.Count == 0)
        {
            return Math.Round(requiredScore, 2);
        }

        var preferredMatches = job.PreferredSkills
            .Count(skill =>
                candidateSkills.Contains(_skillCanonicalizer.GetCanonicalKey(skill)));

        var preferredScore =
            (decimal)preferredMatches /
            job.PreferredSkills.Count *
            100;

        // Required skills matter substantially more than preferred skills.
        return Math.Round(
            requiredScore * 0.75m +
            preferredScore * 0.25m,
            2);
    }

    private static decimal CalculateExperienceScore(
        CandidateProfile candidate,
        JobProfile job)
    {
        if (!job.MinimumExperienceYears.HasValue)
        {
            return 100;
        }

        var minimum = job.MinimumExperienceYears.Value;

        if (candidate.ExperienceYears < minimum)
        {
            var gap = minimum - candidate.ExperienceYears;

            if (gap >= 2)
            {
                return 30;
            }

            return 65;
        }

        if (job.MaximumExperienceYears.HasValue &&
            candidate.ExperienceYears > job.MaximumExperienceYears.Value)
        {
            return 85;
        }

        return 100;
    }

    private static decimal CalculateSalaryScore(
        CandidateProfile candidate,
        JobProfile job)
    {
        // Unknown salary should not be treated as a rejection.
        if (!job.SalaryMinLpa.HasValue &&
            !job.SalaryMaxLpa.HasValue)
        {
            return 60;
        }

        var maxSalary =
            job.SalaryMaxLpa ??
            job.SalaryMinLpa!.Value;

        var minSalary =
            job.SalaryMinLpa ??
            maxSalary;

        if (maxSalary < candidate.MinimumSalaryLpa)
        {
            return 20;
        }

        if (minSalary >= candidate.TargetSalaryLpa)
        {
            return 100;
        }

        if (maxSalary >= candidate.TargetSalaryLpa)
        {
            return 90;
        }

        if (maxSalary >= candidate.MinimumSalaryLpa)
        {
            return 70;
        }

        return 20;
    }

    private static decimal CalculateLocationScore(
        CandidateProfile candidate,
        JobProfile job)
    {
        if (string.IsNullOrWhiteSpace(job.Location))
        {
            return 50;
        }

        if (IsDelhiNcr(job.Location))
        {
            return 100;
        }

        if (job.RemoteAvailable && candidate.OpenToRemote)
        {
            return 100;
        }

        if (job.RelocationAvailable && candidate.OpenToRelocation)
        {
            return 80;
        }

        return 20;
    }

    private static decimal CalculateOverallScore(
        decimal technicalScore,
        decimal experienceScore,
        decimal salaryScore,
        decimal locationScore)
    {
        // Initial V0.1 weighting.
        //
        // Technical      40%
        // Experience     25%
        // Salary         15%
        // Location       10%
        // Other           10% reserved for future company/job-fit signals

        return
            technicalScore * 0.40m +
            experienceScore * 0.25m +
            salaryScore * 0.15m +
            locationScore * 0.10m +
            50m * 0.10m;
    }

    private static bool IsExperienceHardBlocker(
        CandidateProfile candidate,
        JobProfile job)
    {
        if (!job.MinimumExperienceYears.HasValue)
        {
            return false;
        }

        var gap =
            job.MinimumExperienceYears.Value -
            candidate.ExperienceYears;

        // Two or more years below a stated minimum
        // is treated as a hard blocker in V0.1.
        return gap >= 2;
    }

    private static bool IsLocationHardBlocker(
        CandidateProfile candidate,
        JobProfile job)
    {
        if (string.IsNullOrWhiteSpace(job.Location))
        {
            return false;
        }

        if (IsDelhiNcr(job.Location))
        {
            return false;
        }

        if (job.RemoteAvailable && candidate.OpenToRemote)
        {
            return false;
        }

        if (job.RelocationAvailable && candidate.OpenToRelocation)
        {
            return false;
        }

        return true;
    }

    private static string GetRecommendation(
        decimal score,
        bool hasHardBlocker)
    {
        if (hasHardBlocker)
        {
            return "LOW_PRIORITY";
        }

        return score switch
        {
            >= 85 => "STRONGLY_RECOMMENDED",
            >= 70 => "RECOMMENDED",
            >= 55 => "POTENTIAL",
            _ => "LOW_PRIORITY"
        };
    }

    private static IReadOnlyCollection<string> BuildReasons(
        CandidateProfile candidate,
        JobProfile job,
        IReadOnlyCollection<string> matchedSkills,
        IReadOnlyCollection<string> missingSkills,
        IReadOnlyCollection<string> mandatoryGaps,
        decimal experienceScore,
        decimal salaryScore)
    {
        var reasons = new List<string>();

        if (matchedSkills.Count > 0)
        {
            reasons.Add(
                $"Matches {matchedSkills.Count} required skill(s).");
        }

        if (missingSkills.Count > 0)
        {
            reasons.Add(
                $"Missing {missingSkills.Count} required skill(s).");
        }

        if (mandatoryGaps.Count > 0)
        {
            reasons.Add(
                "One or more mandatory requirements are missing.");
        }

        if (experienceScore >= 90)
        {
            reasons.Add("Experience requirement is well aligned.");
        }

        if (salaryScore >= 90)
        {
            reasons.Add("Salary range aligns with target compensation.");
        }

        return reasons;
    }

    private static bool IsDelhiNcr(string location)
    {
        var value = location.ToLowerInvariant();

        return value.Contains("delhi") ||
               value.Contains("gurugram") ||
               value.Contains("gurgaon") ||
               value.Contains("noida") ||
               value.Contains("greater noida") ||
               value.Contains("faridabad") ||
               value.Contains("ghaziabad");
    }
}