using JobApplication = PersonalJobAgent.Domain.Entities.Application;
using PersonalJobAgent.Domain.Enums;

namespace PersonalJobAgent.UnitTests;

public sealed class ApplicationTests
{
    [Fact]
    public void UpdateStatus_AllowsNormalLifecycleProgression()
    {
        var application = CreateApplication();

        application.UpdateStatus(ApplicationStatus.Shortlisted);
        application.UpdateStatus(ApplicationStatus.ReadyToApply);
        application.UpdateStatus(ApplicationStatus.Applied);
        application.UpdateStatus(ApplicationStatus.Interview);

        Assert.Equal(ApplicationStatus.Interview, application.Status);
        Assert.NotNull(application.AppliedAtUtc);
    }

    [Fact]
    public void UpdateStatus_RejectsBackwardTransition()
    {
        var application = CreateApplication();
        application.UpdateStatus(ApplicationStatus.Applied);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            application.UpdateStatus(ApplicationStatus.Shortlisted));

        Assert.Contains("Applied to Shortlisted", exception.Message);
    }

    [Fact]
    public void UpdateStatus_RejectsChangesAfterRejection()
    {
        var application = CreateApplication();
        application.UpdateStatus(ApplicationStatus.Rejected);

        Assert.Throws<InvalidOperationException>(() =>
            application.UpdateStatus(ApplicationStatus.Applied));
    }

    [Fact]
    public void UpdateStatus_StoresPendingActionAndDueDate()
    {
        var application = CreateApplication();
        var dueDate = DateTime.UtcNow.AddDays(2);

        application.UpdateStatus(ApplicationStatus.Applied);

        application.UpdateStatus(
            ApplicationStatus.Assessment,
            "Complete technical assessment",
            dueDate);

        Assert.Equal("Complete technical assessment", application.NextAction);
        Assert.Equal(dueDate, application.NextActionDateUtc);
    }

    private static JobApplication CreateApplication() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        ApplicationStatus.Discovered,
        "https://example.com/apply");
}