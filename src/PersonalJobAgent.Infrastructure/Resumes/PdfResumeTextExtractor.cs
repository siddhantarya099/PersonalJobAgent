using UglyToad.PdfPig;
using PersonalJobAgent.Application.Interfaces;

namespace PersonalJobAgent.Infrastructure.Resumes;

public sealed class PdfResumeTextExtractor : IPdfResumeTextExtractor
{
    public Task<string> ExtractTextAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        using var document = PdfDocument.Open(pdfStream);
        var text = string.Join(
            Environment.NewLine,
            document.GetPages().Select(page => page.Text));

        return Task.FromResult(text.Trim());
    }
}