namespace PersonalJobAgent.Application.Interfaces;

public interface IPendingActionNotificationService
{
    Task<int> NotifyPendingAsync(CancellationToken cancellationToken = default);
}