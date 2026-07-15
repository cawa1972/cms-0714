namespace CMS.API.Security;

/// <summary>
/// Supplies the symmetric JWT signing secret (SysConfig configKey = 'appConfig' → the
/// <c>symmetricSecurityKey</c> property). One provider feeds both token <em>issuance</em>
/// (AuthController) and token <em>validation</em> (JWT bearer middleware) so a single key governs both.
/// </summary>
public interface ISigningKeyProvider
{
    /// <summary>Returns the signing secret, or null if it cannot be resolved.</summary>
    string? GetSigningKey();
}
