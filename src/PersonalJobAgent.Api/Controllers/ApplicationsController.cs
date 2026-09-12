using Microsoft.AspNetCore.Mvc;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Domain.Entities;
using PersonalJobAgent.Domain.Enums;
using JobApplication = PersonalJobAgent.Domain.Entities.Application;

namespace PersonalJobAgent.Api.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPendingActionNotificationService _pendingActionNotificationService;
    private readonly IResumeRepository _resumeRepository;

    public ApplicationsController(
        IApplicationRepository applicationRepository,
        ICandidateRepository candidateRepository,
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        IPendingActionNotificationService pendingActionNotificationService,
        IResumeRepository resumeRepository)
    {
        _applicationRepository = applicationRepository;
        _candidateRepository = candidateRepository;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _pendingActionNotificationService = pendingActionNotificationService;
        _resumeRepository = resumeRepository;
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Create(
        [FromBody] CreateApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
        {
            return NotFound(new { message = "Job not found." });
        }

        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        var existing = await _applicationRepository.GetByJobAndCandidateAsync(
            job.Id,
            candidate.Id,
            cancellationToken);
        if (existing != null)
        {
            return Conflict(new { message = "An application already exists for this job." });
        }

        var application = new JobApplication(
            candidate.Id,
            job.Id,
            ApplicationStatus.Discovered,
            request.ApplicationUrl);

        if (request.ResumeVersionId.HasValue)
        {
            var resumeVersion = await _resumeRepository.GetVersionByIdAsync(
                request.ResumeVersionId.Value,
                cancellationToken);
            if (resumeVersion == null || resumeVersion.Resume.CandidateId != candidate.Id)
            {
                return BadRequest(new { message = "Resume version does not belong to the active candidate." });
            }

            application.AttachResumeVersion(resumeVersion.Id);
        }

        await _applicationRepository.AddAsync(application, cancellationToken);
        var creationEvent = new ApplicationEvent(
                application.Id,
                ApplicationStatus.Unknown,
                ApplicationStatus.Discovered,
                "User",
                "Application record created.");
        application.RecordEvent(creationEvent);
        await _applicationRepository.AddEventAsync(creationEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { applicationId = application.Id }, ToResponse(application));
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<ApplicationResponse>> Get(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, cancellationToken);
        return application == null
            ? NotFound()
            : Ok(ToResponse(application));
    }

    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] ApplicationStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");
        }

        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        var result = await _applicationRepository.GetPageAsync(
            candidate.Id,
            status,
            page,
            pageSize,
            cancellationToken);

        return Ok(new
        {
            applications = result.Applications.Select(ToResponse).ToList(),
            page,
            pageSize,
            totalCount = result.TotalCount
        });
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyCollection<ApplicationResponse>>> Pending(
        CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        var applications = await _applicationRepository.GetPendingAsync(
            candidate.Id,
            DateTime.UtcNow,
            cancellationToken);

        return Ok(applications.Select(ToResponse).ToList());
    }

    [HttpPost("pending/notify")]
    public async Task<ActionResult<object>> NotifyPending(
        CancellationToken cancellationToken = default)
    {
        var notifiedCount = await _pendingActionNotificationService.NotifyPendingAsync(cancellationToken);
        return Ok(new { notifiedCount });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApplicationSummaryResponse>> Summary(
        CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        return Ok(await _applicationRepository.GetSummaryAsync(
            candidate.Id,
            DateTime.UtcNow,
            cancellationToken));
    }

    [HttpPatch("{applicationId:guid}/status")]
    public async Task<ActionResult<ApplicationResponse>> UpdateStatus(
        Guid applicationId,
        [FromBody] UpdateApplicationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, cancellationToken);
        if (application == null)
        {
            return NotFound();
        }

        if (request.Status == ApplicationStatus.Unknown)
        {
            return BadRequest(new { message = "A valid application status is required." });
        }

        var oldStatus = application.Status;
        try
        {
            application.UpdateStatus(
                request.Status,
                request.Action,
                request.NextActionDateUtc);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (oldStatus != request.Status)
        {
            var statusEvent = new ApplicationEvent(
                    application.Id,
                    oldStatus,
                    request.Status,
                    request.Source,
                    string.IsNullOrWhiteSpace(request.Description)
                        ? $"Status changed from {oldStatus} to {request.Status}."
                        : request.Description);
            application.RecordEvent(statusEvent);
            await _applicationRepository.AddEventAsync(statusEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(ToResponse(application));
    }

    private static ApplicationResponse ToResponse(JobApplication application) => new()
    {
        Id = application.Id,
        JobId = application.JobId,
        CandidateId = application.CandidateId,
        Status = application.Status,
        AppliedAtUtc = application.AppliedAtUtc,
        ApplicationUrl = application.ApplicationUrl,
        NextAction = application.NextAction,
        LastStatusChangeUtc = application.LastStatusChangeUtc,
        Events = application.Events.Select(applicationEvent => new ApplicationEventResponse
        {
            OldStatus = applicationEvent.OldStatus,
            NewStatus = applicationEvent.NewStatus,
            Source = applicationEvent.Source,
            Description = applicationEvent.Description,
            OccurredAtUtc = applicationEvent.OccurredAtUtc
        }).ToList()
    };
}