using System.Net;
using System.Text.Json;
using PersonalJobAgent.Application.JobDiscovery.Models;
using PersonalJobAgent.Infrastructure.JobDiscovery;

namespace PersonalJobAgent.UnitTests.JobDiscovery;

public sealed class JsonJobFeedSourceTests
{
    [Fact]
    public async Task FetchJobsAsync_ReturnsParsedJobs_OnSuccess()
    {
        // Arrange
        var jobs = new List<DiscoveredJob>
        {
            new() { ExternalId = "1", Title = "Job 1", Company = "Company A", Description = "Desc 1" },
            new() { ExternalId = "2", Title = "Job 2", Company = "Company B", Description = "Desc 2" }
        };
        var json = JsonSerializer.Serialize(jobs);
        
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });
        
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        var source = new JsonJobFeedSource(httpClient, "https://example.com/feed");

        // Act
        var result = await source.FetchJobsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, j => j.ExternalId == "1");
        Assert.Contains(result, j => j.ExternalId == "2");
    }

    [Fact]
    public async Task FetchJobsAsync_ThrowsException_OnNonSuccessStatusCode()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        });
        
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        var source = new JsonJobFeedSource(httpClient, "https://example.com/feed");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => source.FetchJobsAsync());
    }

    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }
}
