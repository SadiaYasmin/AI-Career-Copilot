namespace CareerCopilot.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CareerCopilot";
    public string Audience { get; set; } = "CareerCopilot";
    public int ExpiryMinutes { get; set; } = 10080;
}