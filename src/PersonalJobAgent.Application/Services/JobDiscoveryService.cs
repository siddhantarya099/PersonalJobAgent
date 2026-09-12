using System.Security.Cryptography;
using System.Text;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobDiscovery.Models;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Services;

public sealed class JobDiscoveryService : IJobDiscoveryService
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JobDiscoveryService(IJobRepository jobRepository, IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<Guid>> ImportAsync(
        IJobSource source,
        CancellationToken cancellationToken = default)
    {
        var discoveredJobs = await source.FetchJobsAsync(cancellationToken);
        var importedJobIds = new List<Guid>();

        foreach (var discoveredJob in discoveredJobs)
        {
            if (string.IsNullOrWhiteSpace(discoveredJob.ExternalId) ||
                string.IsNullOrWhiteSpace(discoveredJob.Description))
            {
                continue;
            }

            var contentHash = ComputeContentHash(discoveredJob.Description);
            if (await _jobRepository.GetByExternalIdAsync(discoveredJob.ExternalId, cancellationToken) != null ||
                await _jobRepository.GetByContentHashAsync(contentHash, cancellationToken) != null)
            {
                continue;
            }

            var job = new Job(
                discoveredJob.ExternalId,
                discoveredJob.Source,
                discoveredJob.Company,
                discoveredJob.Title,
                discoveredJob.Description,
                discoveredJob.Location,
                discoveredJob.SalaryMinLpa,
                discoveredJob.SalaryMaxLpa,
                discoveredJob.JobUrl,
                discoveredJob.PostedAtUtc,
                contentHash);

            await _jobRepository.AddAsync(job, cancellationToken);
            importedJobIds.Add(job.Id);
        }

        if (importedJobIds.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return importedJobIds;
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }
}