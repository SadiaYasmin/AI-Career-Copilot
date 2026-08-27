using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CareerCopilot.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings__DefaultConnection"] = Environment.GetEnvironmentVariable("DB_CONN") ??
                    "Host=localhost;Port=5432;Database=careercopilot;Username=postgres;Password=postgres"
            })
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new ApplicationDbContext(options);
    }
}