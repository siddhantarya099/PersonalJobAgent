namespace PersonalJobAgent.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationMessage(
    string Title,
    string Body,
    string? ActionUrl = null);