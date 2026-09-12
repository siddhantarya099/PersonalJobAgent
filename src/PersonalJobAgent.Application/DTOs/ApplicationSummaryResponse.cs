using PersonalJobAgent.Domain.Enums;

namespace PersonalJobAgent.Application.DTOs;

public sealed class ApplicationSummaryResponse
{
    public int Total { get; set; }

    public IReadOnlyDictionary<ApplicationStatus, int> ByStatus { get; set; } =
        new Dictionary<ApplicationStatus, int>();

    public int PendingActions { get; set; }
}