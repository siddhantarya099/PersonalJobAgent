using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobDiscovery.Models;
using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Application.JobMatching.Services;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Api.Controllers;

[ApiController]
[Route("api/job-matching")]
public sealed class JobMatchingController : ControllerBase
{
    private readonly IJobMatchingEngine _matchingEngine;
    private readonly IJdParserService _jdParserService;
    private readonly IJobAnalysisService _jobAnalysisService;
    private readonly IJobDiscoveryService _jobDiscoveryService;
    private readonly IJobRepository _jobRepository;
    private readonly IJobMatchRepository _jobMatchRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JobMatchingController(
        IJobMatchingEngine matchingEngine,
        IJdParserService jdParserService,
        IJobAnalysisService jobAnalysisService,
        IJobDiscoveryService jobDiscoveryService,
        IJobRepository jobRepository,
        IJobMatchRepository jobMatchRepository,
        ICandidateRepository candidateRepository,
        IUnitOfWork unitOfWork)
    {
        _matchingEngine = matchingEngine;
        _jdParserService = jdParserService;
        _jobAnalysisService = jobAnalysisService;
        _jobDiscoveryService = jobDiscoveryService;
        _jobRepository = jobRepository;
        _jobMatchRepository = jobMatchRepository;
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Legacy endpoint: Evaluate a job match in-memory without persistence.
    /// </summary>
    [HttpPost("evaluate")]
    public ActionResult<JobMatchResult> Evaluate(
        [FromBody] JobMatchingRequest request)
    {
        var result = _matchingEngine.Evaluate(
            request.Candidate,
            request.Job);

        return Ok(result);
    }

    /// <summary>
    /// Create and persist a new job.
    /// </summary>
    [HttpPost("jobs")]
    public async Task<ActionResult<JobResponse>> CreateJob(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            return BadRequest("ExternalId is required.");
        }

        // Check if job already exists by ExternalId
        var existingJob = await _jobRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken);
        if (existingJob != null)
        {
            return Conflict(new { message = "Job with this external ID already exists." });
        }

        // Calculate content hash
        var contentHash = ComputeContentHash(request.Description);

        // Create job entity
        var job = new Job(
            externalId: request.ExternalId,
            source: request.Source,
            company: request.Company,
            title: request.Title,
            description: request.Description,
            location: request.Location,
            salaryMinLpa: request.SalaryMinLpa,
            salaryMaxLpa: request.SalaryMaxLpa,
            jobUrl: request.JobUrl,
            postedAtUtc: request.PostedAtUtc,
            contentHash: contentHash);

        // Persist
        await _jobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return response
        var response = new JobResponse
        {
            Id = job.Id,
            ExternalId = job.ExternalId,
            Source = job.Source,
            Company = job.Company,
            Title = job.Title,
            Location = job.Location,
            SalaryMinLpa = job.SalaryMinLpa,
            SalaryMaxLpa = job.SalaryMaxLpa,
            JobUrl = job.JobUrl,
            PostedAtUtc = job.PostedAtUtc,
            CreatedAtUtc = job.CreatedAtUtc
        };

        return CreatedAtAction(nameof(GetJob), new { jobId = job.Id }, response);
    }

    /// <summary>
    /// Parse a raw job description with AI, match it against the active candidate, and persist both records.
    /// </summary>
    [HttpPost("analyze-raw")]
    public async Task<ActionResult<JobMatchResponse>> AnalyzeRawJob(
        [FromBody] AnalyzeRawJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalId) ||
            string.IsNullOrWhiteSpace(request.JobDescription))
        {
            return BadRequest("ExternalId and JobDescription are required.");
        }

        var existingJob = await _jobRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken);
        if (existingJob != null)
        {
            return Conflict(new { message = "Job with this external ID already exists." });
        }

        try
        {
            var jobProfile = await _jdParserService.ParseAsync(
                request.JobDescription,
                cancellationToken);

            var job = new Job(
                externalId: request.ExternalId,
                source: request.Source,
                company: request.Company,
                title: request.Title,
                description: request.JobDescription,
                location: request.Location,
                salaryMinLpa: request.SalaryMinLpa,
                salaryMaxLpa: request.SalaryMaxLpa,
                jobUrl: request.JobUrl,
                postedAtUtc: request.PostedAtUtc,
                contentHash: ComputeContentHash(request.JobDescription));

            await _jobRepository.AddAsync(job, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var jobMatch = await _jobAnalysisService.AnalyzeJobAsync(
                job,
                jobProfile,
                cancellationToken);

            return Ok(new JobMatchResponse
            {
                JobMatchId = jobMatch.Id,
                JobId = jobMatch.JobId,
                CandidateId = jobMatch.CandidateId,
                MatchResult = ToMatchResult(jobMatch),
                CreatedAtUtc = jobMatch.CreatedAtUtc
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Import a batch from an approved feed or operator-provided source.
    /// </summary>
    [HttpPost("discover")]
    public async Task<ActionResult<IReadOnlyCollection<Guid>>> DiscoverJobs(
        [FromBody] DiscoverJobsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Jobs.Count == 0)
        {
            return BadRequest("At least one job is required.");
        }

        var importedJobIds = await _jobDiscoveryService.ImportAsync(
            new RequestJobSource(request.Jobs),
            cancellationToken);

        return Ok(new
        {
            importedCount = importedJobIds.Count,
            jobIds = importedJobIds
        });
    }

    /// <summary>
    /// Get a job by ID.
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<JobResponse>> GetJob(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            return NotFound();
        }

        var response = new JobResponse
        {
            Id = job.Id,
            ExternalId = job.ExternalId,
            Source = job.Source,
            Company = job.Company,
            Title = job.Title,
            Location = job.Location,
            SalaryMinLpa = job.SalaryMinLpa,
            SalaryMaxLpa = job.SalaryMaxLpa,
            JobUrl = job.JobUrl,
            PostedAtUtc = job.PostedAtUtc,
            CreatedAtUtc = job.CreatedAtUtc
        };

        return Ok(response);
    }

    /// <summary>
    /// Get persisted jobs, newest first.
    /// </summary>
    [HttpGet("jobs")]
    public async Task<ActionResult<JobListResponse>> GetJobs(
        [FromQuery] string? search = null,
        [FromQuery] string? source = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");
        }

        var result = await _jobRepository.GetPageAsync(
            search,
            source,
            page,
            pageSize,
            cancellationToken);

        return Ok(new JobListResponse
        {
            Jobs = result.Jobs.Select(job => new JobResponse
            {
                Id = job.Id,
                ExternalId = job.ExternalId,
                Source = job.Source,
                Company = job.Company,
                Title = job.Title,
                Location = job.Location,
                SalaryMinLpa = job.SalaryMinLpa,
                SalaryMaxLpa = job.SalaryMaxLpa,
                JobUrl = job.JobUrl,
                PostedAtUtc = job.PostedAtUtc,
                CreatedAtUtc = job.CreatedAtUtc
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        });
    }

    /// <summary>
    /// Analyze a job and return the match result with the active candidate.
    /// </summary>
    [HttpPost("jobs/{jobId}/analyze")]
    public async Task<ActionResult<JobMatchResponse>> AnalyzeJob(
        Guid jobId,
        [FromBody] AnalyzeJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (ContainsSwaggerPlaceholder(request.JobProfile))
        {
            return BadRequest("Replace Swagger placeholder values such as 'string' with actual job requirements.");
        }

        // Get the job
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            return NotFound(new { message = "Job not found." });
        }

        try
        {
            // Analyze the job
            var jobMatch = await _jobAnalysisService.AnalyzeJobAsync(
                job,
                request.JobProfile,
                cancellationToken);

            var response = new JobMatchResponse
            {
                JobMatchId = jobMatch.Id,
                JobId = jobMatch.JobId,
                CandidateId = jobMatch.CandidateId,
                MatchResult = ToMatchResult(jobMatch),
                CreatedAtUtc = jobMatch.CreatedAtUtc
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get the match result for a job and the active candidate.
    /// </summary>
    [HttpGet("jobs/{jobId}/match")]
    public async Task<ActionResult<JobMatchResponse>> GetJobMatch(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        // Get the job
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            return NotFound(new { message = "Job not found." });
        }

        // Get the active candidate
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        // Get the match for this job and candidate
        var jobMatch = await _jobMatchRepository.GetByJobAndCandidateAsync(jobId, candidate.Id, cancellationToken);
        if (jobMatch == null)
        {
            return NotFound(new { message = "No match found for this job." });
        }

        var response = new JobMatchResponse
        {
            JobMatchId = jobMatch.Id,
            JobId = jobMatch.JobId,
            CandidateId = jobMatch.CandidateId,
            MatchResult = ToMatchResult(jobMatch),
            CreatedAtUtc = jobMatch.CreatedAtUtc
        };

        return Ok(response);
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes);
    }

    private static bool ContainsSwaggerPlaceholder(JobProfile jobProfile)
    {
        return jobProfile.RequiredSkills.Any(IsSwaggerPlaceholder) ||
               jobProfile.PreferredSkills.Any(IsSwaggerPlaceholder) ||
               jobProfile.MandatorySkills.Any(IsSwaggerPlaceholder);
    }

    private static bool IsSwaggerPlaceholder(string value) =>
        string.Equals(value.Trim(), "string", StringComparison.OrdinalIgnoreCase);

    private static JobMatchResult ToMatchResult(JobMatch jobMatch) => new()
    {
        OverallScore = jobMatch.OverallScore,
        TechnicalScore = jobMatch.TechnicalScore,
        ExperienceScore = jobMatch.ExperienceScore,
        SalaryScore = jobMatch.SalaryScore,
        LocationScore = jobMatch.LocationScore,
        MatchedSkills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jobMatch.MatchedSkillsJson) ?? [],
        MissingSkills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jobMatch.MissingSkillsJson) ?? [],
        MandatoryGaps = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jobMatch.MandatoryGapsJson) ?? [],
        HasHardBlocker = jobMatch.Recommendation == Domain.Enums.Recommendation.LowPriority,
        Recommendation = jobMatch.Recommendation.ToString(),
        Reasons = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jobMatch.ResumeStrategyJson) ?? []
    };

    private sealed class RequestJobSource(IReadOnlyCollection<DiscoveredJob> jobs) : IJobSource
    {
        public Task<IReadOnlyCollection<DiscoveredJob>> FetchJobsAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(jobs);
    }
}