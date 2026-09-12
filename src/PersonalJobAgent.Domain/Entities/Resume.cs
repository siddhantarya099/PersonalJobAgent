using PersonalJobAgent.Domain.Common;

namespace PersonalJobAgent.Domain.Entities;

public sealed class Resume : BaseEntity
{
	public Guid CandidateId { get; private set; }

	public string Name { get; private set; } = string.Empty;

	private readonly List<ResumeVersion> _versions = [];

	public IReadOnlyCollection<ResumeVersion> Versions => _versions.AsReadOnly();

	private Resume()
	{
	}

	public Resume(Guid candidateId, string name)
	{
		CandidateId = candidateId;
		Name = name;
	}

	public void AddVersion(ResumeVersion version)
	{
		_versions.Add(version);
	}
}
