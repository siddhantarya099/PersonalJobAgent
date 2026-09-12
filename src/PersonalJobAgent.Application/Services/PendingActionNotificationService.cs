using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Services;

public sealed class PendingActionNotificationService : IPendingActionNotificationService
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public PendingActionNotificationService(
        ICandidateRepository candidateRepository,
        IApplicationRepository applicationRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _candidateRepository = candidateRepository;
        _applicationRepository = applicationRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> NotifyPendingAsync(CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return 0;
        }

        var applications = await _applicationRepository.GetPendingAsync(
            candidate.Id,
            DateTime.UtcNow,
            cancellationToken);

        var notificationsSent = 0;
        foreach (var application in applications)
        {
            var alreadyNotified = application.Events.Any(applicationEvent =>
                applicationEvent.Source == "System:PendingAction" &&
                applicationEvent.OccurredAtUtc >= application.LastStatusChangeUtc);

            if (alreadyNotified)
            {
                continue;
            }

            await _notificationService.NotifyAsync(
                new NotificationMessage(
                    "Pending application action",
                    string.IsNullOrWhiteSpace(application.NextAction)
                        ? $"Application {application.Id} needs attention."
                        : application.NextAction,
                    $"/api/applications/{application.Id}"),
                cancellationToken);

            var notificationEvent = new ApplicationEvent(
                    application.Id,
                    application.Status,
                    application.Status,
                    "System:PendingAction",
                    "Pending action notification sent.");
            application.RecordEvent(notificationEvent);
            await _applicationRepository.AddEventAsync(notificationEvent, cancellationToken);
            notificationsSent++;
        }

        if (notificationsSent > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return notificationsSent;
    }
}