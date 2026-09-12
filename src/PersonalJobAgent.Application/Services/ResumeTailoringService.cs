using System.Text.Json;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Services;

public sealed class ResumeTailoringService : IResumeTailoringService
{
    private const string SystemPrompt = """
        Tailor a resume for a job using only facts explicitly present in the master resume.
        Never invent employers, dates, achievements, responsibilities, skills, metrics, or education.
        You may reorder or rephrase existing facts for relevance and ATS clarity.
        Return only JSON with a Content string containing the tailored resume.
        If a requested skill is absent from the master resume, do not add it.
        """;

    private readonly IAiService _aiService;
    private readonly IResumeRepository _resumeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResumeTailoringService(
        IAiService aiService,
        IResumeRepository resumeRepository,
        IUnitOfWork unitOfWork)
    {
        _aiService = aiService;
        _resumeRepository = resumeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumeVersion> CreateTailoredVersionAsync(
        Resume resume,
        JobProfile jobProfile,
        JobMatchResult matchResult,
        CancellationToken cancellationToken = default)
    {
        var masterVersion = resume.Versions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();
        if (masterVersion == null)
        {
            throw new InvalidOperationException("A master resume version is required.");
        }

        var request = JsonSerializer.Serialize(new
        {
            masterResume = masterVersion.Content,
            jobProfile,
            matchResult
        });

        var draft = await _aiService.GenerateStructuredResponseAsync<TailoredResumeDraft>(
            SystemPrompt,
            request,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(draft.Content))
        {
            throw new InvalidOperationException("AI returned an empty tailored resume.");
        }

        var version = new ResumeVersion(
            resume.Id,
            resume.Versions.Count == 0 ? 1 : resume.Versions.Max(item => item.VersionNumber) + 1,
            draft.Content,
            "AI");
        resume.AddVersion(version);
        await _resumeRepository.AddVersionAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return version;
    }
}