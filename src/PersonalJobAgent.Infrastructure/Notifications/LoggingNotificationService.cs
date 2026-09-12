using Microsoft.Extensions.Logging;
using PersonalJobAgent.Application.Interfaces;

namespace PersonalJobAgent.Infrastructure.Notifications;

public sealed class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Notification: {Title}. {Body} Action: {ActionUrl}",
            message.Title,
            message.Body,
            message.ActionUrl ?? "none");

        return Task.CompletedTask;
    }
}