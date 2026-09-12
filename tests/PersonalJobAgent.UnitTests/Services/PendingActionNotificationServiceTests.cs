using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Application.Services;
using PersonalJobAgent.Domain.Entities;
using PersonalJobAgent.Domain.Enums;
using JobApplication = PersonalJobAgent.Domain.Entities.Application;

namespace PersonalJobAgent.UnitTests.Services;

public sealed class PendingActionNotificationServiceTests
{
    [Fact]
    public async Task NotifyPendingAsync_NotifiesEachPendingActionOnlyOnce()
    {
        var candidate = new Candidate("Candidate", "Engineer", "Company", 3, "Gurugram", 14, 16, 2, true);
        var application = new JobApplication(Guid.NewGuid(), Guid.NewGuid(), ApplicationStatus.Discovered, "https://example.com");
        application.UpdateStatus(ApplicationStatus.Applied);
        application.UpdateStatus(ApplicationStatus.Interview, "Prepare for interview", DateTime.UtcNow.AddHours(-1));

        var repository = new FakeApplicationRepository(application);
        var notifications = new RecordingNotificationService();
        var service = new PendingActionNotificationService(
            new FakeCandidateRepository(candidate),
            repository,
            notifications,
            new RecordingUnitOfWork());

        Assert.Equal(1, await service.NotifyPendingAsync());
        Assert.Equal(0, await service.NotifyPendingAsync());
        Assert.Single(notifications.Messages);
        Assert.Single(application.Events, applicationEvent => applicationEvent.Source == "System:PendingAction");
    }

    private sealed class FakeCandidateRepository(Candidate candidate) : ICandidateRepository
    {
        public Task<Candidate?> GetActiveCandidateAsync(CancellationToken cancellationToken = default) => Task.FromResult<Candidate?>(candidate);
        public Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Candidate?>(candidate);
        public Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Candidate candidate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeApplicationRepository(JobApplication application) : IApplicationRepository
    {
        public Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<JobApplication?>(application);
        public Task<JobApplication?> GetByJobAndCandidateAsync(Guid jobId, Guid candidateId, CancellationToken cancellationToken = default) => Task.FromResult<JobApplication?>(application);
        public Task<(IReadOnlyCollection<JobApplication> Applications, int TotalCount)> GetPageAsync(Guid candidateId, ApplicationStatus? status, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyCollection<JobApplication>, int)>(([], 0));
        public Task<IReadOnlyCollection<JobApplication>> GetPendingAsync(Guid candidateId, DateTime asOfUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<JobApplication>>([application]);
        public Task<ApplicationSummaryResponse> GetSummaryAsync(Guid candidateId, DateTime asOfUtc, CancellationToken cancellationToken = default) => Task.FromResult(new ApplicationSummaryResponse());
        public Task AddAsync(JobApplication application, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddEventAsync(ApplicationEvent applicationEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public List<NotificationMessage> Messages { get; } = [];
        public Task NotifyAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}