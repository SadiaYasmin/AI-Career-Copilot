using CareerCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareerCopilot.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeminiOptions>>().Value);

        services.AddHttpClient<GeminiApiClient>(client => client.Timeout = TimeSpan.FromSeconds(120));

        services.AddScoped<ICareerAiService, AiCareerService>();

        return services;
    }
}