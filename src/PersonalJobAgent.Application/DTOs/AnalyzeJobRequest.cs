using PersonalJobAgent.Application.JobMatching.Models;

namespace PersonalJobAgent.Application.DTOs;

public sealed class AnalyzeJobRequest
{
    public JobProfile JobProfile { get; set; } = new();
}
