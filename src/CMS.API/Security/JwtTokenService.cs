using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CMS.API.Security;

/// <summary>Default <see cref="IJwtTokenService"/>: HS256 tokens with a fixed 24-hour lifetime.</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    /// <summary>Access-token lifetime: expires 24 hours after issue.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public string CreateToken(string userId, string userName, IEnumerable<string> roleIds, string signingKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtClaims.UserId, userId),
            new(JwtClaims.UserName, userName),
        };
        claims.AddRange(roleIds.Select(roleId => new Claim(JwtClaims.Role, roleId)));

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
