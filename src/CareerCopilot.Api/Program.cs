using System.Text;
using CareerCopilot.AI;
using CareerCopilot.API.Common;
using CareerCopilot.Application;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Infrastructure;
using CareerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        ["Ai:ApiKey"] = Environment.GetEnvironmentVariable("AI_API_KEY") ?? "",
        ["Ai:Model"] = Environment.GetEnvironmentVariable("AI_MODEL") ?? "",
        ["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? "",
        ["Jwt:Secret"] = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "",
        ["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "",
        ["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? ""
    }.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value))
    .AddEnvironmentVariables()
    .Build();

var host = new HostBuilder()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.Sources.Clear();
            cfg.AddConfiguration(configuration);
        });
        webBuilder.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://+:8080");
        webBuilder.ConfigureServices(services =>
        {
            services
                .AddApplication(configuration)
                .AddInfrastructure(configuration)
                .AddAi(configuration);

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
            services.AddOpenApi();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<CareerCopilot.Infrastructure.Authentication.JwtOptions>((options, jwt) =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            services.AddAuthorization();

            var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                ?? new[] { "http://localhost:5173" };

            services.AddCors(options => options.AddPolicy("web", policy =>
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()));
        });
        webBuilder.Configure(app =>
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connStr = db.Database.GetConnectionString();
            if (!string.IsNullOrEmpty(connStr))
            {
                try { db.Database.Migrate(); }
                catch (Exception ex)
                {
                    var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
                    logger.LogWarning(ex, "Database migration failed, attempting EnsureCreated");
                    try { db.Database.EnsureCreated(); } catch { }
                }
            }

            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseCors("web");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .UseContentRoot(Directory.GetCurrentDirectory());

await host.Build().RunAsync();

public partial class Program;
