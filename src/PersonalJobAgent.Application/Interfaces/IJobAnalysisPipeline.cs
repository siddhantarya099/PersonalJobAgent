namespace PersonalJobAgent.Application.Interfaces;

public interface IJobAnalysisPipeline
{
    Task<JobAnalysisPipelineResult> ProcessAsync(
        IReadOnlyCollection<Guid> jobIds,
        CancellationToken cancellationToken = default);
}

public sealed record JobAnalysisPipelineResult(
    int ProcessedCount,
    int AnalyzedCount,
    int SkippedCount,
    int FailedCount);