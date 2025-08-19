using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Infrastructure.Identty;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Jwt;

public class JwtTokenServce
{
    private readonly IConfiguration _cfg;
    private readonly AppDbContext _db;

    public JwtTokenServce(IConfiguration cfg, AppDbContext db)
    {
        _cfg = cfg;
        _db = db;
    }

    public async Task<(string AccessToken, DateTime ExpiresAt, string RefreshToken, DateTime RefreshExpiresAt)>
        IssueAsync(AppUser user, IEnumerable<string> roles, RefreshToken? rotateFrom = null)
    {
        var issuer = _cfg["JWT:Issuer"] ?? "issuer";
        var audience = _cfg["JWT:Audience"] ?? "aud";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["JWT:Key"] ?? "dev-key"));
        var accessMinutes = int.TryParse(_cfg["JWT:AccessTokenMinutes"], out var m) ? m : 15;
        var refreshDays = int.TryParse(_cfg["JWT:RefreshTokenDays"], out var d) ? d : 7;

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expires = DateTime.UtcNow.AddMinutes(accessMinutes);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var access = new JwtSecurityTokenHandler().WriteToken(token);

        if (rotateFrom != null)
        {
            rotateFrom.RevokedAt = DateTime.UtcNow;
            _db.RefreshTokens.Attach(rotateFrom);
            _db.Entry(rotateFrom).Property(x => x.RevokedAt).IsModified = true;
        }

        var refresh = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            UserId = user.Id
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();

        return (access, expires, refresh.Token, refresh.ExpiresAt);
    }
}

