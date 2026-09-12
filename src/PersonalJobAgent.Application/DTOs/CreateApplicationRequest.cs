namespace PersonalJobAgent.Application.DTOs;

public sealed class CreateApplicationRequest
{
    public Guid JobId { get; set; }

    public string ApplicationUrl { get; set; } = string.Empty;
    public Guid? ResumeVersionId { get; set; }
}