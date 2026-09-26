using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobDiscovery.Models;
using System.Text.Json.Serialization;

namespace PersonalJobAgent.Infrastructure.JobDiscovery;

public sealed class AdzunaJobSource : IJobSource
{
    private readonly HttpClient _httpClient;
    private readonly AdzunaOptions _options;

    public AdzunaJobSource(
        HttpClient httpClient,
        IOptions<AdzunaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyCollection<DiscoveredJob>> FetchJobsAsync(
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        var jobs = new List<DiscoveredJob>();

        foreach (var keyword in _options.Keywords)
        {
            foreach (var location in _options.Locations)
            {
                for (var page = 1; page <= _options.MaxPages; page++)
                {
                    var requestUri = BuildRequestUri(
                        keyword,
                        location,
                        page);

                    using var response = await _httpClient.GetAsync(
                        requestUri,
                        cancellationToken);

                    response.EnsureSuccessStatusCode();

                    await using var stream =
                        await response.Content.ReadAsStreamAsync(
                            cancellationToken);

                    var result =
                        await JsonSerializer.DeserializeAsync<AdzunaSearchResponse>(
                            stream,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            },
                            cancellationToken);

                    if (result?.Results == null ||
                        result.Results.Count == 0)
                    {
                        break;
                    }

                    jobs.AddRange(
                        result.Results
                            .Where(IsUsable)
                            .Select(MapJob));

                    if (result.Results.Count < _options.ResultsPerPage)
                    {
                        break;
                    }
                }
            }
        }

        return jobs;
    }

    private Uri BuildRequestUri(
        string keyword,
        string location,
        int page)
    {
        var query =
            $"app_id={Uri.EscapeDataString(_options.AppId)}" +
            $"&app_key={Uri.EscapeDataString(_options.AppKey)}" +
            $"&results_per_page={_options.ResultsPerPage}" +
            $"&what={Uri.EscapeDataString(keyword)}" +
            $"&where={Uri.EscapeDataString(location)}" +
            "&full_time=1" +
            "&permanent=1" +
            "&content-type=application/json";

        return new Uri(
            $"https://api.adzuna.com/v1/api/jobs/" +
            $"{Uri.EscapeDataString(_options.Country)}/search/{page}?{query}");
    }

    private static bool IsUsable(AdzunaJob job)
    {
        return !string.IsNullOrWhiteSpace(job.Id) &&
               !string.IsNullOrWhiteSpace(job.Title) &&
               !string.IsNullOrWhiteSpace(job.Description);
    }

    private static DiscoveredJob MapJob(AdzunaJob job)
    {
        return new DiscoveredJob
        {
            ExternalId = $"adzuna:{job.Id}",
            Source = "Adzuna",
            Company = job.Company?.DisplayName ?? string.Empty,
            Title = job.Title ?? string.Empty,
            Description = job.Description ?? string.Empty,
            Location = job.Location?.DisplayName ?? string.Empty,
            SalaryMinLpa = ToLpa(job.SalaryMin),
            SalaryMaxLpa = ToLpa(job.SalaryMax),
            JobUrl = job.RedirectUrl ?? string.Empty,
            PostedAtUtc = ParseCreatedDate(job.Created)
        };
    }

    private static decimal? ToLpa(decimal? annualSalary)
    {
        if (!annualSalary.HasValue || annualSalary.Value <= 0)
        {
            return null;
        }

        return Math.Round(
            annualSalary.Value / 100000m,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static DateTime? ParseCreatedDate(string? created)
    {
        if (DateTimeOffset.TryParse(
                created,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed.UtcDateTime;
        }

        return null;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.AppId))
        {
            throw new InvalidOperationException(
                "Adzuna AppId is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.AppKey))
        {
            throw new InvalidOperationException(
                "Adzuna AppKey is not configured.");
        }

        if (_options.ResultsPerPage <= 0)
        {
            throw new InvalidOperationException(
                "Adzuna ResultsPerPage must be greater than zero.");
        }

        if (_options.MaxPages <= 0)
        {
            throw new InvalidOperationException(
                "Adzuna MaxPages must be greater than zero.");
        }
    }

    private sealed class AdzunaSearchResponse
    {
        public List<AdzunaJob> Results { get; init; } = [];
    }

    private sealed class AdzunaJob
    {
        public string? Id { get; init; }

        public string? Title { get; init; }

        public string? Description { get; init; }

        [JsonPropertyName("redirect_url")]
        public string? RedirectUrl { get; init; }

        public string? Created { get; init; }

        [JsonPropertyName("salary_min")]
        public decimal? SalaryMin { get; init; }

        [JsonPropertyName("salary_max")]
        public decimal? SalaryMax { get; init; }

        public AdzunaCompany? Company { get; init; }

        public AdzunaLocation? Location { get; init; }
    }

    private sealed class AdzunaCompany
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }
    }

    private sealed class AdzunaLocation
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }
    }
}