using Microsoft.Extensions.Logging;
using PersonalJobAgent.Application.Interfaces;

namespace PersonalJobAgent.Application.Services;

public sealed class JobAnalysisPipeline : IJobAnalysisPipeline
{
    private readonly IJobRepository _jobRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IJobMatchRepository _jobMatchRepository;
    private readonly IJdParserService _jdParserService;
    private readonly IJobAnalysisService _jobAnalysisService;
    private readonly JobDescriptionEnrichmentService _enrichmentService;
    private readonly ILogger<JobAnalysisPipeline> _logger;

    public JobAnalysisPipeline(
        IJobRepository jobRepository,
        ICandidateRepository candidateRepository,
        IJobMatchRepository jobMatchRepository,
        IJdParserService jdParserService,
        IJobAnalysisService jobAnalysisService,
        JobDescriptionEnrichmentService enrichmentService,
        ILogger<JobAnalysisPipeline> logger)
    {
        _jobRepository = jobRepository;
        _candidateRepository = candidateRepository;
        _jobMatchRepository = jobMatchRepository;
        _jdParserService = jdParserService;
        _jobAnalysisService = jobAnalysisService;
        _enrichmentService = enrichmentService;
        _logger = logger;
    }

    public async Task<JobAnalysisPipelineResult> ProcessAsync(
        IReadOnlyCollection<Guid> jobIds,
        CancellationToken cancellationToken = default)
    {
        var processedCount = 0;
        var analyzedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;
        var deferredCount = 0;

        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);

        if (candidate == null)
        {
            throw new InvalidOperationException(
                "No active candidate found in the database.");
        }

        foreach (var jobId in jobIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            processedCount++;

            try
            {
                var job = await _jobRepository.GetByIdAsync(
                    jobId,
                    cancellationToken);

                if (job == null)
                {
                    failedCount++;

                    _logger.LogWarning(
                        "Job {JobId} returned by discovery could not be found.",
                        jobId);

                    continue;
                }

                var existingMatch =
                    await _jobMatchRepository.GetByJobAndCandidateAsync(
                        job.Id,
                        candidate.Id,
                        cancellationToken);

                if (existingMatch != null)
                {
                    skippedCount++;

                    _logger.LogDebug(
                        "Skipping job {JobId} because a match already exists.",
                        job.Id);

                    continue;
                }

                if (job.Source.Equals("Adzuna", StringComparison.OrdinalIgnoreCase) && 
                job.Description.Length >= 500)
                {
                    bool enriched = await _enrichmentService.EnrichAsync(job.Id, cancellationToken);
                    
                    if (!enriched)
                    {
                        deferredCount++;

                        _logger.LogInformation("Deferring analysis for job {JobId} because the Adzuna description appears to be truncated and could not be enriched.",
                        job.Id);

                        continue;
                    }
                    
                    // Reload job to get the updated description
                    job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
                    if (job == null)
                    {
                        failedCount++;
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(job.Description))
                {
                    failedCount++;

                    _logger.LogWarning(
                        "Skipping analysis for job {JobId} because the job description is empty.",
                        job.Id);

                    continue;
                }

                var jobProfile = await _jdParserService.ParseAsync(
                    job.Description,
                    cancellationToken);

                await _jobAnalysisService.AnalyzeJobAsync(
                    job,
                    jobProfile,
                    cancellationToken);

                analyzedCount++;

                _logger.LogInformation(
                    "Successfully analyzed job {JobId} ({Title} at {Company}).",
                    job.Id,
                    job.Title,
                    job.Company);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failedCount++;

                _logger.LogError(
                    exception,
                    "Failed to analyze job {JobId}.",
                    jobId);
            }
        }

        return new JobAnalysisPipelineResult(
            processedCount,
            analyzedCount,
            skippedCount,
            deferredCount,
            failedCount);
    }
}