using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobMatching.Models;

namespace PersonalJobAgent.Application.Services;

public sealed class JdParserService : IJdParserService
{
    private const string SystemPrompt = """
        You extract structured job requirements from job descriptions.
        Return only a JSON object compatible with the JobProfile schema.
        Use empty strings, nulls, empty arrays, and false when information is absent.
        Put core requirements in RequiredSkills, optional requirements in PreferredSkills,
        and skills explicitly described as mandatory in MandatorySkills.
        Preserve the original job description in Description.
        Do not invent requirements, salary, experience, location, company, or title.
        """;

    private readonly IAiService _aiService;

    public JdParserService(IAiService aiService)
    {
        _aiService = aiService;
    }

    public Task<JobProfile> ParseAsync(
        string jobDescription,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            throw new ArgumentException("Job description is required.", nameof(jobDescription));
        }

        return _aiService.GenerateStructuredResponseAsync<JobProfile>(
            SystemPrompt,
            jobDescription,
            cancellationToken);
    }
}