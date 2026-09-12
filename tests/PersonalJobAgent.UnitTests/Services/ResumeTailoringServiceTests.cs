using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Application.Services;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.UnitTests.Services;

public sealed class ResumeTailoringServiceTests
{
    [Fact]
    public async Task CreateTailoredVersionAsync_CreatesAiVersionFromMasterContent()
    {
        var resume = new Resume(Guid.NewGuid(), "Master Resume");
        var masterVersion = new ResumeVersion(resume.Id, 1, "Python experience", "User");
        resume.AddVersion(masterVersion);
        var aiService = new RecordingAiService("Tailored Python resume");
        var service = new ResumeTailoringService(
            aiService,
            new RecordingResumeRepository(),
            new RecordingUnitOfWork());

        var version = await service.CreateTailoredVersionAsync(
            resume,
            new JobProfile { RequiredSkills = ["Python"] },
            new JobMatchResult { MatchedSkills = ["Python"] });

        Assert.Equal(2, version.VersionNumber);
        Assert.Equal("AI", version.Source);
        Assert.Equal("Tailored Python resume", version.Content);
        Assert.Contains("Python experience", aiService.UserPrompt);
        Assert.Contains("RequiredSkills", aiService.UserPrompt);
    }

    [Fact]
    public async Task CreateTailoredVersionAsync_RejectsEmptyAiResult()
    {
        var resume = new Resume(Guid.NewGuid(), "Master Resume");
        resume.AddVersion(new ResumeVersion(resume.Id, 1, "Master content", "User"));
        var service = new ResumeTailoringService(
            new RecordingAiService(string.Empty),
            new RecordingResumeRepository(),
            new RecordingUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTailoredVersionAsync(
            resume,
            new JobProfile(),
            new JobMatchResult()));
    }

    private sealed class RecordingAiService(string content) : IAiService
    {
        public string UserPrompt { get; private set; } = string.Empty;

        public Task<T> GenerateStructuredResponseAsync<T>(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            UserPrompt = userPrompt;
            object response = new TailoredResumeDraft { Content = content };
            return Task.FromResult((T)response);
        }
    }

    private sealed class RecordingResumeRepository : IResumeRepository
    {
        public Task<Resume?> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) => Task.FromResult<Resume?>(null);
        public Task<ResumeVersion?> GetVersionByIdAsync(Guid resumeVersionId, CancellationToken cancellationToken = default) => Task.FromResult<ResumeVersion?>(null);
        public Task AddAsync(Resume resume, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersionAsync(ResumeVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}