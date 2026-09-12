using Microsoft.AspNetCore.Mvc;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Api.Controllers;

[ApiController]
[Route("api/resumes")]
public sealed class ResumesController : ControllerBase
{
    private readonly IResumeRepository _resumeRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobMatchRepository _jobMatchRepository;
    private readonly IResumeTailoringService _resumeTailoringService;
    private readonly IPdfResumeTextExtractor _pdfResumeTextExtractor;
    private readonly ICandidateProfileExtractionService _candidateProfileExtractionService;

    public ResumesController(
        IResumeRepository resumeRepository,
        ICandidateRepository candidateRepository,
        IUnitOfWork unitOfWork,
        IJobMatchRepository jobMatchRepository,
        IResumeTailoringService resumeTailoringService,
        IPdfResumeTextExtractor pdfResumeTextExtractor,
        ICandidateProfileExtractionService candidateProfileExtractionService)
    {
        _resumeRepository = resumeRepository;
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
        _jobMatchRepository = jobMatchRepository;
        _resumeTailoringService = resumeTailoringService;
        _pdfResumeTextExtractor = pdfResumeTextExtractor;
        _candidateProfileExtractionService = candidateProfileExtractionService;
    }

    [HttpPost]
    public async Task<ActionResult<ResumeResponse>> Create(
        [FromBody] CreateResumeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Resume content is required.");
        }

        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        if (await _resumeRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken) != null)
        {
            return Conflict(new { message = "A master resume already exists for the active candidate." });
        }

        var resume = new Resume(candidate.Id, request.Name);
        var version = new ResumeVersion(resume.Id, 1, request.Content, request.Source);
        resume.AddVersion(version);
        await _resumeRepository.AddAsync(resume, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { resumeId = resume.Id }, ToResponse(resume));
    }

    [HttpGet("{resumeId:guid}")]
    public async Task<ActionResult<ResumeResponse>> Get(
        Guid resumeId,
        CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        var resume = await _resumeRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        return resume == null || resume.Id != resumeId
            ? NotFound()
            : Ok(ToResponse(resume));
    }

    [HttpGet("current")]
    public async Task<ActionResult<ResumeResponse>> GetCurrent(
        CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        var resume = await _resumeRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        return resume == null ? NotFound() : Ok(ToResponse(resume));
    }

    [HttpPost("upload-pdf")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ResumeResponse>> UploadPdf(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0 ||
            !string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Upload a non-empty PDF resume.");
        }

        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        if (candidate == null)
        {
            return BadRequest(new { message = "No active candidate found." });
        }

        if (await _resumeRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken) != null)
        {
            return Conflict(new { message = "A master resume already exists for the active candidate." });
        }

        await using var stream = file.OpenReadStream();
        var content = await _pdfResumeTextExtractor.ExtractTextAsync(stream, cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest("The PDF did not contain extractable text.");
        }

        var resume = new Resume(candidate.Id, "Master Resume");
        var version = new ResumeVersion(resume.Id, 1, content, "User PDF Upload");
        resume.AddVersion(version);
        await _resumeRepository.AddAsync(resume, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { resumeId = resume.Id }, ToResponse(resume));
    }

    [HttpPost("{resumeId:guid}/versions")]
    public async Task<ActionResult<ResumeVersionResponse>> AddVersion(
        Guid resumeId,
        [FromBody] CreateResumeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Resume content is required.");
        }

        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        var resume = candidate == null
            ? null
            : await _resumeRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        if (resume == null || resume.Id != resumeId)
        {
            return NotFound();
        }

        var versionNumber = resume.Versions.Count == 0
            ? 1
            : resume.Versions.Max(version => version.VersionNumber) + 1;
        var version = new ResumeVersion(resume.Id, versionNumber, request.Content, request.Source);
        resume.AddVersion(version);
        await _resumeRepository.AddVersionAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new ResumeVersionResponse
        {
            Id = version.Id,
            VersionNumber = version.VersionNumber,
            Content = version.Content,
            Source = version.Source,
            CreatedAtUtc = version.CreatedAtUtc
        });
    }

    [HttpPost("{resumeId:guid}/tailor")]
    public async Task<ActionResult<ResumeVersionResponse>> Tailor(
        Guid resumeId,
        [FromBody] TailorResumeRequest request,
        CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        var resume = candidate == null
            ? null
            : await _resumeRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        if (resume == null || resume.Id != resumeId)
        {
            return NotFound();
        }

        var jobMatch = candidate == null
            ? null
            : await _jobMatchRepository.GetByJobAndCandidateAsync(
                request.JobId,
                candidate.Id,
                cancellationToken);
        if (jobMatch == null)
        {
            return BadRequest(new { message = "Analyze the job before tailoring a resume." });
        }

        try
        {
            var version = await _resumeTailoringService.CreateTailoredVersionAsync(
                resume,
                request.JobProfile,
                ToMatchResult(jobMatch),
                cancellationToken);

            return Ok(new ResumeVersionResponse
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                Content = version.Content,
                Source = version.Source,
                CreatedAtUtc = version.CreatedAtUtc
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{resumeId:guid}/extract-profile")]
    public async Task<ActionResult<ExtractedCandidateProfile>> ExtractProfile(
        Guid resumeId,
        CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetActiveCandidateAsync(cancellationToken);
        var resume = candidate == null
            ? null
            : await _resumeRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        if (resume == null || resume.Id != resumeId)
        {
            return NotFound();
        }

        try
        {
            return Ok(await _candidateProfileExtractionService.ExtractAsync(
                resume,
                cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static ResumeResponse ToResponse(Resume resume) => new()
    {
        Id = resume.Id,
        CandidateId = resume.CandidateId,
        Name = resume.Name,
        Versions = resume.Versions.Select(version => new ResumeVersionResponse
        {
            Id = version.Id,
            VersionNumber = version.VersionNumber,
            Content = version.Content,
            Source = version.Source,
            CreatedAtUtc = version.CreatedAtUtc
        }).ToList()
    };

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
}