using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Options;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Services;

public sealed class AuthService(
    SupportDbContext db,
    IOptions<AuthOptions> options,
    IClock clock) : IAuthService
{
    private readonly PasswordHasher<Agent> _hasher = new();

    public async Task<LoginResponse?> SignInAsync(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var agent = await db.Set<Agent>()
            .FirstOrDefaultAsync(a => a.Email.ToLower() == email, ct);

        // Same failure for missing agent, wrong password, or inactive: null.
        if (agent is null || agent.PasswordHash is null || !agent.IsActive)
        {
            return null;
        }

        var verification = _hasher.VerifyHashedPassword(agent, agent.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // If the hasher algorithm upgraded, re-store the hash. (Identity does this for us.)
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            agent.SetPasswordHash(_hasher.HashPassword(agent, request.Password));
            await db.SaveChangesAsync(ct);
        }

        var token = BuildToken(agent, out var expiresAt);

        return new LoginResponse(
            token,
            expiresAt,
            new AuthenticatedUserDto(agent.Id, agent.FullName, agent.Email));
    }

    public async Task<AuthenticatedUserDto?> GetUserAsync(int agentId, CancellationToken ct)
    {
        var agent = await db.Set<Agent>()
            .AsNoTracking()
            .Where(a => a.Id == agentId && a.IsActive)
            .Select(a => new AuthenticatedUserDto(a.Id, a.FullName, a.Email))
            .FirstOrDefaultAsync(ct);

        return agent;
    }

    private string BuildToken(Agent agent, out DateTime expiresAtUtc)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.JwtKey) || opts.JwtKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Auth:JwtKey is missing or too short (must be at least 32 characters). " +
                "Set it in appsettings or the Auth__JwtKey environment variable.");
        }

        var now = clock.UtcNow;
        expiresAtUtc = now.AddHours(opts.TokenLifetimeHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, agent.Email),
            new Claim(JwtRegisteredClaimNames.Name, agent.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}