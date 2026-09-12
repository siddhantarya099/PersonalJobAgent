using System.Text.Json;
using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Services;

public sealed class CandidateProfileExtractionService : ICandidateProfileExtractionService
{
    private const string SystemPrompt = """
        Extract a candidate profile from a resume.
        Return only JSON compatible with ExtractedCandidateProfile.
        Use only facts explicitly present in the resume. Never invent skills, years, employers, or education.
        Include evidence for every extracted skill using a short quote or faithful summary from the resume.
        """;

    private readonly IAiService _aiService;

    public CandidateProfileExtractionService(IAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<ExtractedCandidateProfile> ExtractAsync(
        Resume resume,
        CancellationToken cancellationToken = default)
    {
        var version = resume.Versions
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("A resume version is required.");

        var profile = await _aiService.GenerateStructuredResponseAsync<ExtractedCandidateProfile>(
            SystemPrompt,
            JsonSerializer.Serialize(new { resume = version.Content }),
            cancellationToken);

        return profile ?? throw new InvalidOperationException("AI returned an empty candidate profile.");
    }
}