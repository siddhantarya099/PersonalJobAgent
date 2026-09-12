using System.Text.Json;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobDiscovery.Models;

namespace PersonalJobAgent.Infrastructure.JobDiscovery;

public sealed class JsonJobFeedSource : IJobSource
{
    private readonly HttpClient _httpClient;
    private readonly Uri _feedUri;

    public JsonJobFeedSource(HttpClient httpClient, string feedUrl)
    {
        if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var feedUri) ||
            feedUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("The job feed URL must be an absolute HTTPS URL.", nameof(feedUrl));
        }

        _httpClient = httpClient;
        _feedUri = feedUri;
    }

    public async Task<IReadOnlyCollection<DiscoveredJob>> FetchJobsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(_feedUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var jobs = await JsonSerializer.DeserializeAsync<List<DiscoveredJob>>(
            contentStream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return jobs ?? [];
    }
}