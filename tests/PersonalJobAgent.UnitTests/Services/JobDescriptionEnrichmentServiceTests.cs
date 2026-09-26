using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.Services;
using PersonalJobAgent.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace PersonalJobAgent.UnitTests.Services;

public class JobDescriptionEnrichmentServiceTests
{
    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesCalled { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeJobRepository : IJobRepository
    {
        private readonly Job? _jobToReturn;
        public bool UpdateCalled { get; private set; }

        public FakeJobRepository(Job? jobToReturn = null)
        {
            _jobToReturn = jobToReturn;
        }

        public Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_jobToReturn);
        public Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyCollection<Job> Jobs, int TotalCount)> GetPageAsync(string? search, string? source, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Job?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Job?> GetByContentHashAsync(string contentHash, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(Job job, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        
        public Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEnricher : IJobDescriptionEnricher
    {
        private readonly bool _canEnrich;
        private readonly string? _enrichResult;

        public bool EnrichCalled { get; private set; }

        public FakeEnricher(bool canEnrich, string? enrichResult)
        {
            _canEnrich = canEnrich;
            _enrichResult = enrichResult;
        }

        public bool CanEnrich(Job job) => _canEnrich;

        public Task<string?> EnrichAsync(Job job, CancellationToken cancellationToken = default)
        {
            EnrichCalled = true;
            return Task.FromResult(_enrichResult);
        }
    }

    [Fact]
    public async Task EnrichAsync_JobNotFound_ReturnsFalse()
    {
        var jobRepo = new FakeJobRepository(null);
        var uow = new FakeUnitOfWork();
        var service = new JobDescriptionEnrichmentService(jobRepo, uow, Enumerable.Empty<IJobDescriptionEnricher>());

        var result = await service.EnrichAsync(Guid.NewGuid());

        Assert.False(result);
        Assert.False(jobRepo.UpdateCalled);
    }

    [Fact]
    public async Task EnrichAsync_NoEnricherSupportsJob_ReturnsFalse()
    {
        var job = new Job("originalId", "source", "company", "title", "desc", "loc", null, null, "url", null, "hash");
        var jobRepo = new FakeJobRepository(job);
        var uow = new FakeUnitOfWork();
        var enricher = new FakeEnricher(false, "result");
        
        var service = new JobDescriptionEnrichmentService(jobRepo, uow, new[] { enricher });

        var result = await service.EnrichAsync(job.Id);

        Assert.False(result);
        Assert.False(enricher.EnrichCalled);
        Assert.False(jobRepo.UpdateCalled);
    }

    [Fact]
    public async Task EnrichAsync_EnricherReturnsNull_ReturnsFalse()
    {
        var job = new Job("originalId", "source", "company", "title", "desc", "loc", null, null, "url", null, "hash");
        var jobRepo = new FakeJobRepository(job);
        var uow = new FakeUnitOfWork();
        var enricher = new FakeEnricher(true, null);
        
        var service = new JobDescriptionEnrichmentService(jobRepo, uow, new[] { enricher });

        var result = await service.EnrichAsync(job.Id);

        Assert.False(result);
        Assert.True(enricher.EnrichCalled);
        Assert.False(jobRepo.UpdateCalled);
    }

    [Fact]
    public async Task EnrichAsync_EnricherReturnsEmpty_ReturnsFalse()
    {
        var job = new Job("originalId", "source", "company", "title", "desc", "loc", null, null, "url", null, "hash");
        var jobRepo = new FakeJobRepository(job);
        var uow = new FakeUnitOfWork();
        var enricher = new FakeEnricher(true, "   ");
        
        var service = new JobDescriptionEnrichmentService(jobRepo, uow, new[] { enricher });

        var result = await service.EnrichAsync(job.Id);

        Assert.False(result);
        Assert.True(enricher.EnrichCalled);
        Assert.False(jobRepo.UpdateCalled);
    }

    [Fact]
    public async Task EnrichAsync_Success_UpdatesJobAndReturnsTrue()
    {
        var job = new Job("originalId", "source", "company", "title", "desc", "loc", null, null, "url", null, "hash");
        var jobRepo = new FakeJobRepository(job);
        var uow = new FakeUnitOfWork();
        var enricher = new FakeEnricher(true, "full description");
        
        var service = new JobDescriptionEnrichmentService(jobRepo, uow, new[] { enricher });

        var result = await service.EnrichAsync(job.Id);

        Assert.True(result);
        Assert.True(enricher.EnrichCalled);
        Assert.Equal("full description", job.Description);
        
        using var sha256 = SHA256.Create();
        var expectedHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes("full description")));
        Assert.Equal(expectedHash, job.ContentHash);

        Assert.True(jobRepo.UpdateCalled);
        Assert.True(uow.SaveChangesCalled);
    }
}
