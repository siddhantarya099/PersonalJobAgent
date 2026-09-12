namespace PersonalJobAgent.Application.DTOs;

public sealed class CreateJobRequest
{
    public string ExternalId { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal? SalaryMinLpa { get; set; }

    public decimal? SalaryMaxLpa { get; set; }

    public string JobUrl { get; set; } = string.Empty;

    public DateTime? PostedAtUtc { get; set; }
}
