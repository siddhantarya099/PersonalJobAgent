using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using PersonalJobAgent.Infrastructure.JobDiscovery;

namespace PersonalJobAgent.UnitTests.JobDiscovery;

public sealed class AdzunaJobSourceTests
{
    [Fact]
    public async Task FetchJobsAsync_MapsAdzunaResponse()
    {
        var handler = new FakeHttpMessageHandler(
            """
            {
              "results": [
                {
                  "id": "12345",
                  "title": "Data Engineer",
                  "description": "Build data pipelines using Azure Databricks and PySpark.",
                  "redirect_url": "https://example.com/job/12345",
                  "created": "2026-09-25T10:30:00Z",
                  "salary_min": 1200000,
                  "salary_max": 1800000,
                  "company": {
                    "display_name": "Example Company"
                  },
                  "location": {
                    "display_name": "Gurgaon"
                  }
                }
              ]
            }
            """);

        var httpClient = new HttpClient(handler);

        var options = Options.Create(new AdzunaOptions
        {
            AppId = "test-app-id",
            AppKey = "test-app-key",
            Country = "in",
            ResultsPerPage = 20,
            MaxPages = 1,
            Keywords = ["Data Engineer"],
            Locations = ["Gurgaon"]
        });

        var source = new AdzunaJobSource(
            httpClient,
            options);

        var jobs = await source.FetchJobsAsync();

        var job = Assert.Single(jobs);

        Assert.Equal("adzuna:12345", job.ExternalId);
        Assert.Equal("Adzuna", job.Source);
        Assert.Equal("Example Company", job.Company);
        Assert.Equal("Data Engineer", job.Title);
        Assert.Equal(
            "Build data pipelines using Azure Databricks and PySpark.",
            job.Description);
        Assert.Equal("Gurgaon", job.Location);
        Assert.Equal(12m, job.SalaryMinLpa);
        Assert.Equal(18m, job.SalaryMaxLpa);
        Assert.Equal(
            "https://example.com/job/12345",
            job.JobUrl);
        Assert.Equal(
            DateTimeKind.Utc,
            job.PostedAtUtc!.Value.Kind);
    }

    [Fact]
    public async Task FetchJobsAsync_SkipsJobsMissingRequiredFields()
    {
        var handler = new FakeHttpMessageHandler(
            """
            {
              "results": [
                {
                  "id": "1",
                  "title": "Valid Job",
                  "description": "Valid description"
                },
                {
                  "id": "2",
                  "title": "Missing Description",
                  "description": ""
                },
                {
                  "id": "",
                  "title": "Missing Id",
                  "description": "Valid description"
                }
              ]
            }
            """);

        var source = CreateSource(handler);

        var jobs = await source.FetchJobsAsync();

        var job = Assert.Single(jobs);

        Assert.Equal("adzuna:1", job.ExternalId);
        Assert.Equal("Valid Job", job.Title);
    }

    [Fact]
    public async Task FetchJobsAsync_StopsPaginationWhenPageIsNotFull()
    {
        var handler = new FakeHttpMessageHandler(
            """
            {
              "results": [
                {
                  "id": "1",
                  "title": "Data Engineer",
                  "description": "Description"
                }
              ]
            }
            """);

        var source = CreateSource(
            handler,
            resultsPerPage: 20,
            maxPages: 5);

        var jobs = await source.FetchJobsAsync();

        Assert.Single(jobs);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task FetchJobsAsync_ThrowsWhenCredentialsAreMissing()
    {
        var handler = new FakeHttpMessageHandler("{}");

        var options = Options.Create(new AdzunaOptions
        {
            AppId = "",
            AppKey = "",
            Keywords = ["Data Engineer"],
            Locations = ["Gurgaon"]
        });

        var source = new AdzunaJobSource(
            new HttpClient(handler),
            options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.FetchJobsAsync());

        Assert.Contains(
            "Adzuna AppId is not configured.",
            exception.Message);

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task FetchJobsAsync_ThrowsWhenApiReturnsError()
    {
        var handler = new FakeHttpMessageHandler(
            "{}",
            HttpStatusCode.Unauthorized);

        var source = CreateSource(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => source.FetchJobsAsync());

        Assert.Equal(1, handler.RequestCount);
    }

    private static AdzunaJobSource CreateSource(
        FakeHttpMessageHandler handler,
        int resultsPerPage = 20,
        int maxPages = 1)
    {
        var options = Options.Create(new AdzunaOptions
        {
            AppId = "test-app-id",
            AppKey = "test-app-key",
            Country = "in",
            ResultsPerPage = resultsPerPage,
            MaxPages = maxPages,
            Keywords = ["Data Engineer"],
            Locations = ["Gurgaon"]
        });

        return new AdzunaJobSource(
            new HttpClient(handler),
            options);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;

        public int RequestCount { get; private set; }

        public FakeHttpMessageHandler(
            string responseBody,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;

            return Task.FromResult(
                new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(
                        _responseBody,
                        Encoding.UTF8,
                        "application/json")
                });
        }
    }
}