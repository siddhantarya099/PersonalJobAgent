using System.Security.Cryptography;
using System.Text;
using PersonalJobAgent.Application.Interfaces;

namespace PersonalJobAgent.Application.Services;

public sealed class JobDescriptionEnrichmentService
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IJobDescriptionEnricher> _enrichers;

    public JobDescriptionEnrichmentService(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        IEnumerable<IJobDescriptionEnricher> enrichers)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _enrichers = enrichers;
    }

    public async Task<bool> EnrichAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(
            jobId,
            cancellationToken);

        if (job == null)
        {
            return false;
        }

        var enricher = _enrichers.FirstOrDefault(e => e.CanEnrich(job));
        if (enricher == null)
        {
            return false;
        }

        var description = await enricher.EnrichAsync(
            job,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        var contentHash = ComputeContentHash(description);

        job.UpdateDescription(
            description,
            contentHash);

        await _jobRepository.UpdateAsync(
            job,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();

        return Convert.ToBase64String(
            sha256.ComputeHash(
                Encoding.UTF8.GetBytes(content)));
    }
}