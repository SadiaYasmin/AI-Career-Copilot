using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace CareerCopilot.Infrastructure.Authentication;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public string CreateToken(Guid userId, string email, UserRole role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.NameId, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        var expires = _options.ExpiryMinutes > 0
            ? DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes)
            : DateTime.UtcNow.AddDays(7);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}