namespace PersonalJobAgent.Application.DTOs;

public sealed class CreateResumeRequest
{
    public string Name { get; set; } = "Master Resume";

    public string Content { get; set; } = string.Empty;

    public string Source { get; set; } = "User";

    public string? FileName { get; set; }
}