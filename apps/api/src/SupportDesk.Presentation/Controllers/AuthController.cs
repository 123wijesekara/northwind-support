using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Signs an agent in and returns a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        [FromServices] IAuthService auth,
        CancellationToken ct)
    {
        var response = await auth.SignInAsync(request, ct);

        if (response is null)
        {
            // Same status and message for wrong email and wrong password.
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(response);
    }

    /// <summary>Returns the currently signed-in agent.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUserDto>> Me(
        [FromServices] IAuthService auth,
        CancellationToken ct)
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var agentId))
        {
            return Unauthorized();
        }

        var user = await auth.GetUserAsync(agentId, ct);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }
}