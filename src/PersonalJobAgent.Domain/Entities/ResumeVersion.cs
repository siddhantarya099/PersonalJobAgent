using PersonalJobAgent.Domain.Common;

namespace PersonalJobAgent.Domain.Entities;

public sealed class ResumeVersion : BaseEntity
{
    public Guid ResumeId { get; private set; }
    public Resume Resume { get; private set; } = null!;

    public int VersionNumber { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public string Source { get; private set; } = string.Empty;

    private ResumeVersion()
    {
    }

    public ResumeVersion(Guid resumeId, int versionNumber, string content, string source)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Resume content is required.", nameof(content));
        }

        ResumeId = resumeId;
        VersionNumber = versionNumber;
        Content = content;
        Source = source;
    }
}