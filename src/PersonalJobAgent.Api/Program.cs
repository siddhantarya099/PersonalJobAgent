using PersonalJobAgent.Application.Interfaces;
using PersonalJobAgent.Application.JobMatching.Services;
using PersonalJobAgent.Application.Services;
using PersonalJobAgent.Infrastructure;
using PersonalJobAgent.Infrastructure.Persistence;
using PersonalJobAgent.Api.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileDevelopment", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.Configure<JobDiscoveryOptions>(
    builder.Configuration.GetSection(JobDiscoveryOptions.SectionName));
builder.Services.AddHttpClient();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddSingleton<
    ISkillCanonicalizer,
    SkillCanonicalizer>();

builder.Services.AddScoped<
    IJobMatchingEngine,
    JobMatchingEngine>();

builder.Services.AddScoped<
    IJobAnalysisService,
    JobAnalysisService>();

builder.Services.AddScoped<
    IJdParserService,
    JdParserService>();

builder.Services.AddScoped<
    IJobDiscoveryService,
    JobDiscoveryService>();

builder.Services.AddScoped<
    IPendingActionNotificationService,
    PendingActionNotificationService>();

builder.Services.AddScoped<
    IResumeTailoringService,
    ResumeTailoringService>();

builder.Services.AddScoped<
    ICandidateProfileExtractionService,
    CandidateProfileExtractionService>();

builder.Services.AddHostedService<JobDiscoveryWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Seed initial candidate data
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PersonalJobAgentDbContext>();
        await DataSeeder.SeedInitialCandidateAsync(dbContext);
    }
}

app.UseHttpsRedirection();
app.UseCors("MobileDevelopment");

app.MapControllers();

app.Run();