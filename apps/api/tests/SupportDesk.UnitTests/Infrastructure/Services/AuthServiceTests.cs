using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Options;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Services;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Services;

/// <summary>
/// Behavioural tests for AuthService: wrong password, inactive agent, valid sign-in,
/// and token contents. Uses in-memory SQLite so no real database is needed.
/// </summary>
public sealed class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SupportDbContext _db;
    private readonly FixedClock _clock = new();

    private readonly AuthOptions _options = new()
    {
        JwtKey = "test_key_at_least_32_chars_long_0000000000",
        Issuer = "SupportDesk.Tests",
        Audience = "SupportDesk.Tests",
        TokenLifetimeHours = 8
    };

    public AuthServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SupportDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private AuthService Sut() =>
        new(_db, Options.Create(_options), _clock);

    private async Task<Agent> SeedAgentAsync(
        string email = "alex@example.com",
        string password = "correct-password",
        bool active = true)
    {
        var agent = new Agent("Alex Turner", email, 10, _clock.UtcNow);

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Agent>();
        agent.SetPasswordHash(hasher.HashPassword(agent, password));

        if (!active)
        {
            agent.Deactivate();
        }

        _db.Set<Agent>().Add(agent);
        await _db.SaveChangesAsync();
        return agent;
    }

    [Fact]
    public async Task SignIn_WithCorrectCredentials_ReturnsTokenAndUser()
    {
        await SeedAgentAsync();

        var response = await Sut().SignInAsync(
            new LoginRequest("alex@example.com", "correct-password"),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response!.Token));
        Assert.Equal(1, response.User.Id);
        Assert.Equal("Alex Turner", response.User.FullName);
        Assert.Equal("alex@example.com", response.User.Email);
        Assert.True(response.ExpiresAtUtc > _clock.UtcNow);
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_ReturnsNull()
    {
        await SeedAgentAsync();

        var response = await Sut().SignInAsync(
            new LoginRequest("alex@example.com", "wrong-password"),
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task SignIn_WithUnknownEmail_ReturnsNull()
    {
        await SeedAgentAsync();

        var response = await Sut().SignInAsync(
            new LoginRequest("nobody@example.com", "correct-password"),
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task SignIn_WithInactiveAgent_ReturnsNull()
    {
        await SeedAgentAsync(active: false);

        var response = await Sut().SignInAsync(
            new LoginRequest("alex@example.com", "correct-password"),
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task SignIn_IsCaseInsensitiveOnEmail()
    {
        await SeedAgentAsync(email: "Alex@Example.com");

        var response = await Sut().SignInAsync(
            new LoginRequest("alex@example.com", "correct-password"),
            CancellationToken.None);

        Assert.NotNull(response);
    }
}