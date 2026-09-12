using PersonalJobAgent.Domain.Common;

namespace PersonalJobAgent.Domain.Entities;

public sealed class Candidate : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string CurrentRole { get; private set; } = string.Empty;

    public string CurrentCompany { get; private set; } = string.Empty;

    public decimal TotalExperienceYears { get; private set; }

    public string CurrentLocation { get; private set; } = string.Empty;

    public decimal MinimumSalaryLpa { get; private set; }

    public decimal TargetSalaryLpa { get; private set; }

    public int NoticePeriodMonths { get; private set; }

    public bool IsActivelyLooking { get; private set; }

    private readonly List<CandidateSkill> _skills = [];

    public IReadOnlyCollection<CandidateSkill> Skills => _skills.AsReadOnly();

    private Candidate()
    {
    }

    public Candidate(
        string name,
        string currentRole,
        string currentCompany,
        decimal totalExperienceYears,
        string currentLocation,
        decimal minimumSalaryLpa,
        decimal targetSalaryLpa,
        int noticePeriodMonths,
        bool isActivelyLooking)
    {
        Name = name;
        CurrentRole = currentRole;
        CurrentCompany = currentCompany;
        TotalExperienceYears = totalExperienceYears;
        CurrentLocation = currentLocation;
        MinimumSalaryLpa = minimumSalaryLpa;
        TargetSalaryLpa = targetSalaryLpa;
        NoticePeriodMonths = noticePeriodMonths;
        IsActivelyLooking = isActivelyLooking;
    }

    public void AddSkill(CandidateSkill skill)
    {
        _skills.Add(skill);
    }
}