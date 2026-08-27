using System.Reflection;
using CareerCopilot.Application.Common.Behaviors;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Scoring;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareerCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.Configure<MatchScoringOptions>(
            configuration.GetSection(MatchScoringOptions.SectionName));

        services.AddScoped<MatchScoringService>();
        services.AddScoped<SkillGapService>();
        services.AddScoped<InterviewScoringService>();
        services.AddScoped<RecruiterReadinessService>();
        services.AddTransient<ResumeAnalyzerService>();

        services.AddScoped<ProfileSnapshotBuilder>();
        services.AddScoped<JobSnapshotBuilder>();

        return services;
    }
}