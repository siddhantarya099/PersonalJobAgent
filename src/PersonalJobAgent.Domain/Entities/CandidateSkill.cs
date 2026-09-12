using PersonalJobAgent.Domain.Common;

namespace PersonalJobAgent.Domain.Entities;

public sealed class CandidateSkill : BaseEntity
{
    public Guid CandidateId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public decimal YearsOfExperience { get; private set; }

    public string Proficiency { get; private set; } = string.Empty;

    public string Evidence { get; private set; } = string.Empty;

    private CandidateSkill()
    {
    }

    public CandidateSkill(
        Guid candidateId,
        string name,
        string category,
        decimal yearsOfExperience,
        string proficiency,
        string evidence)
    {
        CandidateId = candidateId;
        Name = name;
        Category = category;
        YearsOfExperience = yearsOfExperience;
        Proficiency = proficiency;
        Evidence = evidence;
    }
}