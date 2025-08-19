using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Auth.Infrastructure.Identty;
using Auth.Infrastructure.Jwt;

public class JwtRefreshTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task IssueAccessAndRefresh_ThenRotate_RevokesOldRefresh()
    {
        using var db = CreateDb();

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["JWT:Issuer"] = "test",
            ["JWT:Audience"] = "test",
            ["JWT:Key"] = "TEST_TEST_TEST_TEST_TEST_TEST_123456",
            ["JWT:AccessTokenMinutes"] = "5",
            ["JWT:RefreshTokenDays"] = "7"
        };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        var jwt = new JwtTokenServce(cfg, db);
        var user = new AppUser { Id = Guid.NewGuid(), Email = "u@test.local", UserName = "u@test.local" };

        var (access, exp, refresh, rExp) = await jwt.IssueAsync(user, new[] { "Viewer" });
        access.Should().NotBeNullOrEmpty();
        refresh.Should().NotBeNullOrEmpty();

        var existing = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refresh);
        existing.Should().NotBeNull();
        existing!.IsActive.Should().BeTrue();

        var (access2, exp2, newRefresh, newRExp) = await jwt.IssueAsync(user, new[] { "Viewer" }, existing);
        var rotated = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refresh);
        rotated.Should().NotBeNull();
        rotated!.RevokedAt.Should().NotBeNull();
        newRefresh.Should().NotBe(refresh);
    }
}

