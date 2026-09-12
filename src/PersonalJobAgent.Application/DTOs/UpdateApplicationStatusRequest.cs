using PersonalJobAgent.Domain.Enums;

namespace PersonalJobAgent.Application.DTOs;

public sealed class UpdateApplicationStatusRequest
{
    public ApplicationStatus Status { get; set; }

    public string Action { get; set; } = string.Empty;

    public DateTime? NextActionDateUtc { get; set; }

    public string Source { get; set; } = "User";

    public string Description { get; set; } = string.Empty;
}