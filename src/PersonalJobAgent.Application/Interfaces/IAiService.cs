namespace PersonalJobAgent.Application.Interfaces;

public interface IAiService
{
    Task<T> GenerateStructuredResponseAsync<T>(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}