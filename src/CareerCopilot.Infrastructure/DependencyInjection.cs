using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Infrastructure.Authentication;
using CareerCopilot.Infrastructure.Files;
using CareerCopilot.Infrastructure.Persistence;
using CareerCopilot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareerCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += connectionString.Contains('?') ? "&SslMode=Require" : "?SslMode=Require";
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<JwtOptions>(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value);
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IResumeParserService, ResumeParserService>();

        return services;
    }
}