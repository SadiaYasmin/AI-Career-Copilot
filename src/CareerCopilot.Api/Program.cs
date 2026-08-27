using System.Text;
using CareerCopilot.AI;
using CareerCopilot.API.Common;
using CareerCopilot.Application;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

ApplyEnvironmentOverrides(builder.Configuration);

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddAi(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

static void ApplyEnvironmentOverrides(Microsoft.Extensions.Configuration.IConfigurationBuilder configuration)
{
    var apiKey = Environment.GetEnvironmentVariable("AI_API_KEY");
    var model = Environment.GetEnvironmentVariable("AI_MODEL");
    var dbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION");

    var overrides = new Dictionary<string, string?>();
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        overrides["Ai:ApiKey"] = apiKey;
    }

    if (!string.IsNullOrWhiteSpace(model))
    {
        overrides["Ai:Model"] = model;
    }

    if (!string.IsNullOrWhiteSpace(dbConnection))
    {
        overrides["ConnectionStrings:DefaultConnection"] = dbConnection;
    }

    if (overrides.Count > 0)
    {
        configuration.AddInMemoryCollection(overrides);
    }
}

public partial class Program;