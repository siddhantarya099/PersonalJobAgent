using PersonalJobAgent.Application.DTOs;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Application.Interfaces;

public interface ICandidateProfileExtractionService
{
    Task<ExtractedCandidateProfile> ExtractAsync(
        Resume resume,
        CancellationToken cancellationToken = default);
}