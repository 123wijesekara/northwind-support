namespace SupportDesk.Application.Contracts.Auth;

/// <summary>Body of POST /api/auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>The signed-in agent, as the client sees them.</summary>
public sealed record AuthenticatedUserDto(
    int Id,
    string FullName,
    string Email);

/// <summary>Returned by POST /api/auth/login.</summary>
public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    AuthenticatedUserDto User);