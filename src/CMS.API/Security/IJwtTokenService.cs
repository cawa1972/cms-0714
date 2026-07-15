namespace CMS.API.Security;

/// <summary>
/// Issues signed JWT access tokens for authenticated users. The signing secret is supplied per call
/// (read at runtime from SysConfig) rather than baked into the service.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Builds and signs (HS256) a JWT carrying the user's identity and one role claim per role id.
    /// The token is valid for 24 hours from the moment of issue.
    /// </summary>
    /// <param name="userId">Value for the <c>userId</c> claim (and <c>sub</c>).</param>
    /// <param name="userName">Value for the <c>userName</c> claim.</param>
    /// <param name="roleIds">One <see cref="JwtClaims.Role"/> claim is added per id.</param>
    /// <param name="signingKey">The symmetric signing secret (must be at least 32 bytes for HS256).</param>
    string CreateToken(string userId, string userName, IEnumerable<string> roleIds, string signingKey);
}
