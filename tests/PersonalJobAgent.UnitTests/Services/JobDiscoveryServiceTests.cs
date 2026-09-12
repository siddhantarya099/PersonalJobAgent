using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobDiscovery.Models;
using PersonalJobAgent.Application.Services;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.UnitTests.Services;

public sealed class JobDiscoveryServiceTests
{
    [Fact]
    public async Task ImportAsync_ImportsNewJobsAndSavesOnce()
    {
        var repository = new FakeJobRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var service = new JobDiscoveryService(repository, unitOfWork);

        var importedIds = await service.ImportAsync(new FakeJobSource(
        [CreateJob("source-1", "Python and SQL") ]));

        var importedJob = Assert.Single(importedIds);
        Assert.Contains(repository.Jobs, job => job.Id == importedJob);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task ImportAsync_SkipsExternalIdAndContentDuplicates()
    {
        var existingJob = CreateEntity("existing", "same description");
        var repository = new FakeJobRepository { Jobs = [existingJob] };
        var unitOfWork = new RecordingUnitOfWork();
        var service = new JobDiscoveryService(repository, unitOfWork);

        var importedIds = await service.ImportAsync(new FakeJobSource(
        [
            CreateJob("existing", "different description"),
            CreateJob("new-id", "same description")
        ]));

        Assert.Empty(importedIds);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task ImportAsync_SkipsRecordsWithoutExternalIdOrDescription()
    {
        var repository = new FakeJobRepository();
        var service = new JobDiscoveryService(repository, new RecordingUnitOfWork());

        var importedIds = await service.ImportAsync(new FakeJobSource(
        [
            CreateJob("", "description"),
            CreateJob("missing-description", "")
        ]));

        Assert.Empty(importedIds);
        Assert.Empty(repository.Jobs);
    }

    private static DiscoveredJob CreateJob(string externalId, string description) => new()
    {
        ExternalId = externalId,
        Source = "Test",
        Company = "Example",
        Title = "Data Engineer",
        Description = description,
        JobUrl = "https://example.com/job"
    };

    private static Job CreateEntity(string externalId, string description) => new(
        externalId,
        "Test",
        "Example",
        "Data Engineer",
        description,
        "Gurugram",
        null,
        null,
        "https://example.com/job",
        null,
        Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(description))));

    private sealed class FakeJobSource(IReadOnlyCollection<DiscoveredJob> jobs) : IJobSource
    {
        public Task<IReadOnlyCollection<DiscoveredJob>> FetchJobsAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(jobs);
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeJobRepository : IJobRepository
    {
        public List<Job> Jobs { get; init; } = [];

        public Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Jobs.FirstOrDefault(job => job.Id == id));

        public Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Job>>(Jobs);

        public Task<(IReadOnlyCollection<Job> Jobs, int TotalCount)> GetPageAsync(
            string? search,
            string? source,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var filteredJobs = Jobs
                .Where(job => string.IsNullOrWhiteSpace(search) ||
                              job.Title.Contains(search) ||
                              job.Company.Contains(search) ||
                              job.Location.Contains(search))
                .Where(job => string.IsNullOrWhiteSpace(source) || job.Source == source)
                .ToList();

            return Task.FromResult<(IReadOnlyCollection<Job>, int)>(
                (filteredJobs.Skip((page - 1) * pageSize).Take(pageSize).ToList(), filteredJobs.Count));
        }

        public Task<Job?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Jobs.FirstOrDefault(job => job.ExternalId == externalId));

        public Task<Job?> GetByContentHashAsync(string contentHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Jobs.FirstOrDefault(job => job.ContentHash == contentHash));

        public Task AddAsync(Job job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Job job, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}