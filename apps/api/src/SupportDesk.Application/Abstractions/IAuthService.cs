using SupportDesk.Application.Contracts.Auth;

namespace SupportDesk.Application.Abstractions;

/// <summary>
/// Sign-in and token issuing.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Verifies the credentials and returns a signed token. Returns null for wrong email,
    /// wrong password, or an inactive agent — callers must not distinguish between them.
    /// </summary>
    Task<LoginResponse?> SignInAsync(LoginRequest request, CancellationToken ct);

    /// <summary>The user identified by an authenticated request, or null if not found.</summary>
    Task<AuthenticatedUserDto?> GetUserAsync(int agentId, CancellationToken ct);
}