using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Application.Services;

namespace PersonalJobAgent.UnitTests.Services;

public sealed class JdParserServiceTests
{
    [Fact]
    public async Task ParseAsync_DelegatesDescriptionToAiService()
    {
        var aiService = new RecordingAiService();
        var parser = new JdParserService(aiService);

        var result = await parser.ParseAsync("Senior data engineer with Python and PostgreSQL.");

        Assert.Equal("Senior data engineer with Python and PostgreSQL.", aiService.UserPrompt);
        Assert.Contains("RequiredSkills", aiService.SystemPrompt);
        Assert.Equal("Data Engineer", result.Title);
        Assert.Contains("Python", result.RequiredSkills);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ParseAsync_RejectsBlankDescription(string description)
    {
        var parser = new JdParserService(new RecordingAiService());

        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync(description));
    }

    private sealed class RecordingAiService : IAiService
    {
        public string SystemPrompt { get; private set; } = string.Empty;

        public string UserPrompt { get; private set; } = string.Empty;

        public Task<T> GenerateStructuredResponseAsync<T>(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            SystemPrompt = systemPrompt;
            UserPrompt = userPrompt;

            object response = new JobProfile
            {
                Title = "Data Engineer",
                RequiredSkills = ["Python", "PostgreSQL"]
            };

            return Task.FromResult((T)response);
        }
    }
}