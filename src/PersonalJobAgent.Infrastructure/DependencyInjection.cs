using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Infrastructure.Ai;
using PersonalJobAgent.Infrastructure.Persistence;
using PersonalJobAgent.Infrastructure.Persistence.Repositories;
using PersonalJobAgent.Infrastructure.Notifications;
using PersonalJobAgent.Infrastructure.Resumes;
using PersonalJobAgent.Application.Services;

namespace PersonalJobAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var openAiSection = configuration.GetSection(OpenAiOptions.SectionName);
        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey = openAiSection["ApiKey"] ?? string.Empty;
            options.Model = openAiSection["Model"] ?? options.Model;
        });

        var smtpSection = configuration.GetSection(SmtpOptions.SectionName);
        services.Configure<SmtpOptions>(smtpSection);

        services.AddHttpClient<IAiService, OpenAiService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddDbContext<PersonalJobAgentDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IJobMatchRepository, JobMatchRepository>();
        services.AddScoped<IJobAnalysisPipeline, JobAnalysisPipeline>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IResumeRepository, ResumeRepository>();
        services.AddSingleton<IPdfResumeTextExtractor, PdfResumeTextExtractor>();
        services.AddScoped<INotificationService, SmtpNotificationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
