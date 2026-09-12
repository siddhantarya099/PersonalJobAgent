namespace PersonalJobAgent.Application.DTOs;

public sealed class JobListResponse
{
    public IReadOnlyCollection<JobResponse> Jobs { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}