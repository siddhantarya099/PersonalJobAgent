using Microsoft.Extensions.Logging.Abstractions;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Application.Services;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.UnitTests.Services;

public sealed class JobAnalysisPipelineTests
{
    [Fact]
    public async Task ProcessAsync_WhenJobIsNew_AnalyzesJob()
    {
        // Arrange
        var candidate = CreateCandidate();
        var job = CreateJob();

        var jobRepository = new FakeJobRepository(job);
        var candidateRepository = new FakeCandidateRepository(candidate);
        var jobMatchRepository = new FakeJobMatchRepository();
        var parserService = new FakeJdParserService();
        var analysisService = new FakeJobAnalysisService();

        var pipeline = CreatePipeline(
            jobRepository,
            candidateRepository,
            jobMatchRepository,
            parserService,
            analysisService);

        // Act
        var result = await pipeline.ProcessAsync([job.Id]);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.AnalyzedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);

        Assert.Equal(1, parserService.ParseCallCount);
        Assert.Equal(1, analysisService.AnalyzeCallCount);
        Assert.Same(job, analysisService.LastJob);
    }

    [Fact]
    public async Task ProcessAsync_WhenMatchAlreadyExists_SkipsJob()
    {
        // Arrange
        var candidate = CreateCandidate();
        var job = CreateJob();

        var existingMatch = new JobMatch(
            job.Id,
            candidate.Id,
            90m,
            90m,
            90m,
            90m,
            0m,
            90m,
            Domain.Enums.Recommendation.StronglyRecommended,
            "Already analyzed",
            "[]",
            "[]",
            "[]",
            "[]",
            "1.0");

        var jobRepository = new FakeJobRepository(job);
        var candidateRepository = new FakeCandidateRepository(candidate);
        var jobMatchRepository = new FakeJobMatchRepository(existingMatch);
        var parserService = new FakeJdParserService();
        var analysisService = new FakeJobAnalysisService();

        var pipeline = CreatePipeline(
            jobRepository,
            candidateRepository,
            jobMatchRepository,
            parserService,
            analysisService);

        // Act
        var result = await pipeline.ProcessAsync([job.Id]);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.AnalyzedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);

        Assert.Equal(0, parserService.ParseCallCount);
        Assert.Equal(0, analysisService.AnalyzeCallCount);
    }

    [Fact]
    public async Task ProcessAsync_WhenJobDoesNotExist_CountsAsFailed()
    {
        // Arrange
        var candidate = CreateCandidate();

        var jobRepository = new FakeJobRepository();
        var candidateRepository = new FakeCandidateRepository(candidate);
        var jobMatchRepository = new FakeJobMatchRepository();
        var parserService = new FakeJdParserService();
        var analysisService = new FakeJobAnalysisService();

        var pipeline = CreatePipeline(
            jobRepository,
            candidateRepository,
            jobMatchRepository,
            parserService,
            analysisService);

        // Act
        var result = await pipeline.ProcessAsync([Guid.NewGuid()]);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.AnalyzedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(1, result.FailedCount);

        Assert.Equal(0, parserService.ParseCallCount);
        Assert.Equal(0, analysisService.AnalyzeCallCount);
    }

    [Fact]
    public async Task ProcessAsync_WhenDescriptionIsEmpty_CountsAsFailed()
    {
        // Arrange
        var candidate = CreateCandidate();
        var job = CreateJob(description: string.Empty);

        var jobRepository = new FakeJobRepository(job);
        var candidateRepository = new FakeCandidateRepository(candidate);
        var jobMatchRepository = new FakeJobMatchRepository();
        var parserService = new FakeJdParserService();
        var analysisService = new FakeJobAnalysisService();

        var pipeline = CreatePipeline(
            jobRepository,
            candidateRepository,
            jobMatchRepository,
            parserService,
            analysisService);

        // Act
        var result = await pipeline.ProcessAsync([job.Id]);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.AnalyzedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(1, result.FailedCount);

        Assert.Equal(0, parserService.ParseCallCount);
        Assert.Equal(0, analysisService.AnalyzeCallCount);
    }

    [Fact]
    public async Task ProcessAsync_WhenParserFails_CountsAsFailedAndContinues()
    {
        // Arrange
        var candidate = CreateCandidate();
        var job = CreateJob();

        var jobRepository = new FakeJobRepository(job);
        var candidateRepository = new FakeCandidateRepository(candidate);
        var jobMatchRepository = new FakeJobMatchRepository();
        var parserService = new FakeJdParserService
        {
            ExceptionToThrow = new InvalidOperationException("Parser failed")
        };
        var analysisService = new FakeJobAnalysisService();

        var pipeline = CreatePipeline(
            jobRepository,
            candidateRepository,
            jobMatchRepository,
            parserService,
            analysisService);

        // Act
        var result = await pipeline.ProcessAsync([job.Id]);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.AnalyzedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public async Task ProcessAsync_WhenNoActiveCandidateExists_Throws()
    {
        // Arrange
        var jobRepository = new FakeJobRepository();
        var candidateRepository = new FakeCandidateRepository(null);
        var jobMatchRepository = new FakeJobMatchRepository();
        var parserService = new FakeJdParserService();
        var analysisService = new FakeJobAnalysisService();

        var pipeline = CreatePipeline(
            jobRepository,
            candidateRepository,
            jobMatchRepository,
            parserService,
            analysisService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.ProcessAsync([Guid.NewGuid()]));

        Assert.Equal(
            "No active candidate found in the database.",
            exception.Message);
    }

    private static JobAnalysisPipeline CreatePipeline(
        IJobRepository jobRepository,
        ICandidateRepository candidateRepository,
        IJobMatchRepository jobMatchRepository,
        IJdParserService parserService,
        IJobAnalysisService analysisService)
    {
        return new JobAnalysisPipeline(
            jobRepository,
            candidateRepository,
            jobMatchRepository,
            parserService,
            analysisService,
            NullLogger<JobAnalysisPipeline>.Instance);
    }

    private static Candidate CreateCandidate()
    {
        return new Candidate(
            name: "Test Candidate",
            currentRole: "Software Engineer",
            currentCompany: "Test Company",
            totalExperienceYears: 3m,
            currentLocation: "Delhi NCR",
            minimumSalaryLpa: 14m,
            targetSalaryLpa: 18m,
            noticePeriodMonths: 2,
            isActivelyLooking: true);
    }

    private static Job CreateJob(string description = "Test job description")
    {
        return new Job(
            externalId: Guid.NewGuid().ToString(),
            source: "Test",
            company: "Test Company",
            title: "Software Engineer",
            description: description,
            location: "Gurugram",
            salaryMinLpa: 15m,
            salaryMaxLpa: 20m,
            jobUrl: "https://example.com/job",
            postedAtUtc: DateTime.UtcNow,
            contentHash: Guid.NewGuid().ToString());
    }

    private sealed class FakeJobRepository : IJobRepository
    {
        private readonly Dictionary<Guid, Job> _jobs;

        public FakeJobRepository(params Job[] jobs)
        {
            _jobs = jobs.ToDictionary(job => job.Id);
        }

        public Task<Job?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _jobs.TryGetValue(id, out var job);
            return Task.FromResult(job);
        }

        public Task<IReadOnlyCollection<Job>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Job>>(
                _jobs.Values.ToList());
        }

        public Task<(IReadOnlyCollection<Job> Jobs, int TotalCount)> GetPageAsync(
            string? search,
            string? source,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var jobs = _jobs.Values.ToList();

            return Task.FromResult(
                ((IReadOnlyCollection<Job>)jobs, jobs.Count));
        }

        public Task<Job?> GetByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _jobs.Values.FirstOrDefault(job => job.ExternalId == externalId));
        }

        public Task<Job?> GetByContentHashAsync(
            string contentHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _jobs.Values.FirstOrDefault(job => job.ContentHash == contentHash));
        }

        public Task AddAsync(
            Job job,
            CancellationToken cancellationToken = default)
        {
            _jobs[job.Id] = job;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Job job,
            CancellationToken cancellationToken = default)
        {
            _jobs[job.Id] = job;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCandidateRepository : ICandidateRepository
    {
        private readonly Candidate? _candidate;

        public FakeCandidateRepository(Candidate? candidate)
        {
            _candidate = candidate;
        }

        public Task<Candidate?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _candidate?.Id == id ? _candidate : null);
        }

        public Task<Candidate?> GetActiveCandidateAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_candidate);
        }

        public Task AddAsync(
            Candidate candidate,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Candidate candidate,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJobMatchRepository : IJobMatchRepository
    {
        private readonly List<JobMatch> _matches = [];

        public FakeJobMatchRepository(params JobMatch[] matches)
        {
            _matches.AddRange(matches);
        }

        public Task<JobMatch?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _matches.FirstOrDefault(match => match.Id == id));
        }

        public Task<JobMatch?> GetByJobAndCandidateAsync(
            Guid jobId,
            Guid candidateId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _matches.FirstOrDefault(match =>
                    match.JobId == jobId &&
                    match.CandidateId == candidateId));
        }

        public Task AddAsync(
            JobMatch jobMatch,
            CancellationToken cancellationToken = default)
        {
            _matches.Add(jobMatch);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            JobMatch jobMatch,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJdParserService : IJdParserService
    {
        public int ParseCallCount { get; private set; }

        public Exception? ExceptionToThrow { get; init; }

        public Task<JobProfile> ParseAsync(
            string jobDescription,
            CancellationToken cancellationToken = default)
        {
            ParseCallCount++;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(new JobProfile
            {
                Title = "Software Engineer",
                Company = "Test Company",
                Location = "Gurugram",
                SalaryMinLpa = 12m,
                SalaryMaxLpa = 20m,
                MinimumExperienceYears = 2m,
                MaximumExperienceYears = 5m,
                RequiredSkills = ["C#", ".NET", "SQL"],
                PreferredSkills = [],
                MandatorySkills = [],
                RemoteAvailable = true,
                RelocationAvailable = true,
                Description = jobDescription
            });
        }
    }

    private sealed class FakeJobAnalysisService : IJobAnalysisService
    {
        public int AnalyzeCallCount { get; private set; }

        public Job? LastJob { get; private set; }

        public Task<JobMatch> AnalyzeJobAsync(
            Job job,
            JobProfile jobProfile,
            CancellationToken cancellationToken = default)
        {
            AnalyzeCallCount++;
            LastJob = job;

            var result = new JobMatch(
                job.Id,
                Guid.NewGuid(),
                85m,
                90m,
                90m,
                80m,
                0m,
                90m,
                Domain.Enums.Recommendation.StronglyRecommended,
                "Test analysis",
                "[]",
                "[]",
                "[]",
                "[]",
                "1.0");

            return Task.FromResult(result);
        }
    }
}