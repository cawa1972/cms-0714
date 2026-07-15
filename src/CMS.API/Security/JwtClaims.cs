namespace CMS.API.Security;

/// <summary>Claim type names used in issued JWTs, kept in one place so producers and readers agree.</summary>
public static class JwtClaims
{
    public const string UserId = "userId";
    public const string UserName = "userName";

    /// <summary>Role claim type. Emitted once per assigned RoleId.</summary>
    public const string Role = "role";
}
