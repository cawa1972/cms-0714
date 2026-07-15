namespace CMS.API.Security;

/// <summary>
/// Well-known RoleId values (rows in <c>AppRole</c>) referenced by role-based authorization.
/// The JWT carries one <see cref="JwtClaims.Role"/> claim per assigned RoleId, and
/// <c>TokenValidationParameters.RoleClaimType</c> maps it so <c>[Authorize(Roles = …)]</c> works.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
}
