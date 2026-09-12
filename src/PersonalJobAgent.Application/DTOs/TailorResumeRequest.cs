using PersonalJobAgent.Application.JobMatching.Models;

namespace PersonalJobAgent.Application.DTOs;

public sealed class TailorResumeRequest
{
    public Guid JobId { get; set; }

    public JobProfile JobProfile { get; set; } = new();
}