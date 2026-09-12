using Microsoft.Extensions.Options;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Infrastructure.JobDiscovery;

namespace PersonalJobAgent.Api.Services;

public sealed class JobDiscoveryWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<JobDiscoveryOptions> _options;
    private readonly ILogger<JobDiscoveryWorker> _logger;

    public JobDiscoveryWorker(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<JobDiscoveryOptions> options,
        ILogger<JobDiscoveryWorker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.FeedUrl))
        {
            _logger.LogInformation("Scheduled job discovery is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, options.IntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var discoveryService = scope.ServiceProvider.GetRequiredService<IJobDiscoveryService>();
                var source = new JsonJobFeedSource(
                    _httpClientFactory.CreateClient(),
                    options.FeedUrl);
                var importedIds = await discoveryService.ImportAsync(
                    source,
                    stoppingToken);

                _logger.LogInformation(
                "Scheduled job discovery imported {Count} new jobs.",
                importedIds.Count);
                
                if (importedIds.Count > 0)
                {
                    var analysisPipeline =
                    scope.ServiceProvider.GetRequiredService<IJobAnalysisPipeline>();

                    var analysisResult = await analysisPipeline.ProcessAsync(
                        importedIds,
                        stoppingToken);

                    _logger.LogInformation(
                        "Job analysis pipeline completed. Processed: {ProcessedCount}, " +
                        "Analyzed: {AnalyzedCount}, Skipped: {SkippedCount}, Failed: {FailedCount}.",
                        analysisResult.ProcessedCount,
                        analysisResult.AnalyzedCount,
                        analysisResult.SkippedCount,
                        analysisResult.FailedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Scheduled job discovery failed.");
            }
        }
    }
}