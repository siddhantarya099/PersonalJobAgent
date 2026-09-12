namespace PersonalJobAgent.Application.Interfaces;

public interface IPdfResumeTextExtractor
{
    Task<string> ExtractTextAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default);
}