using CareerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CareerCopilot.IntegrationTests;

/// <summary>
/// Boots the real API (Program.cs) against an in-memory SQLite database.
/// The AI provider is left unconfigured so every AI feature runs its
/// deterministic fallback - tests are offline, fast and reproducible.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private bool _initialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "",
                ["Jwt:Secret"] = "integration-test-secret-key-that-is-long-enough-for-hmac-sha-256",
                ["Jwt:Issuer"] = "CareerCopilot",
                ["Jwt:Audience"] = "CareerCopilot",
                ["Storage:RootPath"] = Path.Combine(
                    Path.GetTempPath(), "careercopilot-tests", Guid.NewGuid().ToString("N"))
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
            services.RemoveAll<ApplicationDbContext>();

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            services.AddSingleton(connection);
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        });
    }

    public new HttpClient CreateClient()
    {
        EnsureDatabase();
        return base.CreateClient();
    }

    private void EnsureDatabase()
    {
        if (_initialized)
        {
            return;
        }

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        }

        _initialized = true;
    }
}