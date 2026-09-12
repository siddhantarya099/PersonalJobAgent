using Microsoft.AspNetCore.Mvc;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobDiscovery.Models;
using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Application.JobMatching.Services;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Api.Controllers;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.UnitTests.Controllers;

public sealed class JobMatchingControllerTests
{
    [Fact]
    public async Task DiscoverJobs_WhenNewJobsAreImported_RunsAnalysisPipeline()
    {
        // Arrange
        var importedJobId = Guid.NewGuid();

        var discoveryService = new FakeJobDiscoveryService(importedJobId);
        var analysisPipeline = new FakeJobAnalysisPipeline(
            new JobAnalysisPipelineResult(
                ProcessedCount: 1,
                AnalyzedCount: 1,
                SkippedCount: 0,
                FailedCount: 0));

        var controller = CreateController(
            discoveryService: discoveryService,
            analysisPipeline: analysisPipeline);

        var request = new DiscoverJobsRequest
        {
            Jobs =
            [
                new DiscoveredJob
                {
                    ExternalId = "job-1",
                    Source = "Test",
                    Company = "Test Company",
                    Title = "Software Engineer",
                    Description = "C# .NET SQL developer",
                    Location = "Gurugram",
                    SalaryMinLpa = 15m,
                    SalaryMaxLpa = 20m,
                    JobUrl = "https://example.com/job-1",
                    PostedAtUtc = DateTime.UtcNow
                }
            ]
        };

        // Act
        var result = await controller.DiscoverJobs(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);

        Assert.Equal(1, discoveryService.ImportCallCount);
        Assert.Equal(1, analysisPipeline.ProcessCallCount);

        Assert.Equal(
            new[] { importedJobId },
            analysisPipeline.LastProcessedJobIds);
    }

    [Fact]
    public async Task DiscoverJobs_WhenNoJobsAreProvided_ReturnsBadRequest()
    {
        // Arrange
        var discoveryService = new FakeJobDiscoveryService();
        var analysisPipeline = new FakeJobAnalysisPipeline(
            new JobAnalysisPipelineResult(0, 0, 0, 0));

        var controller = CreateController(
            discoveryService: discoveryService,
            analysisPipeline: analysisPipeline);

        var request = new DiscoverJobsRequest
        {
            Jobs = []
        };

        // Act
        var result = await controller.DiscoverJobs(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "At least one job is required.",
            badRequest.Value);

        Assert.Equal(0, discoveryService.ImportCallCount);
        Assert.Equal(0, analysisPipeline.ProcessCallCount);
    }

    [Fact]
    public async Task DiscoverJobs_WhenNoNewJobsAreImported_DoesNotRunAnalysisPipeline()
    {
        // Arrange
        var discoveryService = new FakeJobDiscoveryService();
        var analysisPipeline = new FakeJobAnalysisPipeline(
            new JobAnalysisPipelineResult(0, 0, 0, 0));

        var controller = CreateController(
            discoveryService: discoveryService,
            analysisPipeline: analysisPipeline);

        var request = new DiscoverJobsRequest
        {
            Jobs =
            [
                new DiscoveredJob
                {
                    ExternalId = "existing-job",
                    Source = "Test",
                    Company = "Test Company",
                    Title = "Software Engineer",
                    Description = "C# .NET SQL developer",
                    Location = "Gurugram",
                    SalaryMinLpa = 15m,
                    SalaryMaxLpa = 20m,
                    JobUrl = "https://example.com/existing-job",
                    PostedAtUtc = DateTime.UtcNow
                }
            ]
        };

        // Act
        var result = await controller.DiscoverJobs(request);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);

        Assert.Equal(1, discoveryService.ImportCallCount);
        Assert.Equal(0, analysisPipeline.ProcessCallCount);
    }

    private static JobMatchingController CreateController(
        IJobDiscoveryService discoveryService,
        IJobAnalysisPipeline analysisPipeline)
    {
        return new JobMatchingController(
            new FakeJobMatchingEngine(),
            new FakeJdParserService(),
            new FakeJobAnalysisService(),
            discoveryService,
            analysisPipeline,
            new FakeJobRepository(),
            new FakeJobMatchRepository(),
            new FakeCandidateRepository(),
            new FakeUnitOfWork());
    }

    private sealed class FakeJobDiscoveryService : IJobDiscoveryService
    {
        private readonly IReadOnlyCollection<Guid> _importedIds;

        public FakeJobDiscoveryService(params Guid[] importedIds)
        {
            _importedIds = importedIds;
        }

        public int ImportCallCount { get; private set; }

        public Task<IReadOnlyCollection<Guid>> ImportAsync(
            IJobSource source,
            CancellationToken cancellationToken = default)
        {
            ImportCallCount++;
            return Task.FromResult(_importedIds);
        }
    }

    private sealed class FakeJobAnalysisPipeline : IJobAnalysisPipeline
    {
        private readonly JobAnalysisPipelineResult _result;

        public FakeJobAnalysisPipeline(JobAnalysisPipelineResult result)
        {
            _result = result;
        }

        public int ProcessCallCount { get; private set; }

        public IReadOnlyCollection<Guid> LastProcessedJobIds { get; private set; } = [];

        public Task<JobAnalysisPipelineResult> ProcessAsync(
            IReadOnlyCollection<Guid> jobIds,
            CancellationToken cancellationToken = default)
        {
            ProcessCallCount++;
            LastProcessedJobIds = jobIds;

            return Task.FromResult(_result);
        }
    }

    private sealed class FakeJobMatchingEngine : IJobMatchingEngine
    {
        public JobMatchResult Evaluate(
            CandidateProfile candidate,
            JobProfile job)
        {
            return new JobMatchResult
            {
                OverallScore = 0m,
                TechnicalScore = 0m,
                ExperienceScore = 0m,
                SalaryScore = 0m,
                LocationScore = 0m,
                MatchedSkills = [],
                MissingSkills = [],
                MandatoryGaps = [],
                Reasons = [],
                Recommendation = "LOW_PRIORITY"
            };
        }
    }

    private sealed class FakeJdParserService : IJdParserService
    {
        public Task<JobProfile> ParseAsync(
            string jobDescription,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new JobProfile());
        }
    }

    private sealed class FakeJobAnalysisService : IJobAnalysisService
    {
        public Task<JobMatch> AnalyzeJobAsync(
            Job job,
            JobProfile jobProfile,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeJobRepository : IJobRepository
    {
        public Task<Job?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Job?>(null);

        public Task<IReadOnlyCollection<Job>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Job>>([]);

        public Task<(IReadOnlyCollection<Job> Jobs, int TotalCount)> GetPageAsync(
            string? search,
            string? source,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                (Jobs: (IReadOnlyCollection<Job>)[],
                    TotalCount: 0));

        public Task<Job?> GetByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Job?>(null);

        public Task<Job?> GetByContentHashAsync(
            string contentHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Job?>(null);

        public Task AddAsync(
            Job job,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(
            Job job,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeJobMatchRepository : IJobMatchRepository
    {
        public Task<JobMatch?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<JobMatch?>(null);

        public Task<JobMatch?> GetByJobAndCandidateAsync(
            Guid jobId,
            Guid candidateId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<JobMatch?>(null);

        public Task AddAsync(
            JobMatch jobMatch,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(
            JobMatch jobMatch,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeCandidateRepository : ICandidateRepository
    {
        public Task<Candidate?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Candidate?>(null);

        public Task<Candidate?> GetActiveCandidateAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Candidate?>(null);

        public Task AddAsync(
            Candidate candidate,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(
            Candidate candidate,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}