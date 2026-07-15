using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

/// <summary>
/// Authentication endpoints. <c>login</c> is <see cref="AllowAnonymousAttribute"/> so it stays
/// reachable without a token; <c>profile</c> carries no such opt-out, so the global authorization
/// filter protects it — the signed-in user is identified from the JWT, never the request body.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Generic message on every failure so we never reveal which check (user, active, password) failed.
    private const string InvalidCredentialsMessage = "invalid credentials";

    // Change-password messages (bilingual — shown as-is in the UI).
    private const string CurrentPasswordIncorrectMessage =
        "目前密碼錯誤。(Current password is incorrect.)";
    private const string PasswordMismatchMessage =
        "新密碼與確認密碼不一致。(New password and confirmation do not match.)";

    private readonly IAuthRepository _repository;
    private readonly IJwtTokenService _tokenService;
    private readonly ISigningKeyProvider _signingKeyProvider;

    public AuthController(
        IAuthRepository repository,
        IJwtTokenService tokenService,
        ISigningKeyProvider signingKeyProvider)
    {
        _repository = repository;
        _tokenService = tokenService;
        _signingKeyProvider = signingKeyProvider;
    }

    /// <summary>
    /// Authenticates a user against the AppUser table and returns a profile with a signed JWT.
    /// Returns 401 (generic message) if the user is unknown, inactive, or the password is wrong.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var credential = await _repository.GetCredentialAsync(request.UserId);

        // All three checks collapse to the same 401 so the caller cannot tell them apart.
        if (credential is null
            || !credential.IsActive
            || credential.PasswordHash != PasswordHasher.Hash(request.Password))
        {
            return Unauthorized(new { message = InvalidCredentialsMessage });
        }

        var signingKey = _signingKeyProvider.GetSigningKey();
        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException(
                "SysConfig 'appConfig' is missing its 'symmetricSecurityKey'; cannot issue a token.");

        var accessToken = _tokenService.CreateToken(
            credential.UserId, credential.UserName, credential.RoleIds, signingKey);

        return Ok(new LoginResponse
        {
            UserId = credential.UserId,
            UserName = credential.UserName,
            AccessToken = accessToken,
        });
    }

    /// <summary>
    /// Updates the signed-in user's own display name (UserName). The target UserId is taken from the
    /// authenticated token's <see cref="JwtClaims.UserId"/> claim — never from the request body — so a
    /// user can rename only their own account and can never change their UserId or roles. Protected by
    /// the global authorization filter (no <c>[AllowAnonymous]</c> here).
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<UpdateProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        // Identity comes solely from the validated JWT; the request body carries no UserId.
        var userId = User.FindFirst(JwtClaims.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // UserName is required and stored trimmed; whitespace-only is rejected (Required alone allows it).
        var userName = (request.UserName ?? string.Empty).Trim();
        if (userName.Length == 0)
            return BadRequest(new { message = "UserName is required." });

        var updated = await _repository.UpdateUserNameAsync(userId, userName);
        if (!updated)
            return NotFound();

        return Ok(new UpdateProfileResponse { UserId = userId, UserName = userName });
    }

    /// <summary>
    /// Changes the signed-in user's own password. The target UserId comes from the JWT — never the
    /// body. Rejects (400, bilingual message, nothing written) when the current password is wrong,
    /// the new password fails the complexity policy (<see cref="PasswordPolicy"/>), or new/confirm
    /// don't match. On success stores SHA256(new) and stamps PasswordUpdatedTime. No hash is ever
    /// sent to or from the client.
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(JwtClaims.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var credential = await _repository.GetCredentialAsync(userId);
        if (credential is null)
            return NotFound();

        // 1. The current password must hash to the stored PasswordHash.
        if (credential.PasswordHash != PasswordHasher.Hash(request.CurrentPassword))
            return BadRequest(new { message = CurrentPasswordIncorrectMessage });

        // 2. The new password must satisfy the complexity policy.
        if (!PasswordPolicy.IsCompliant(request.NewPassword))
            return BadRequest(new { message = PasswordPolicy.RequirementMessage });

        // 3. New and confirmation must match.
        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new { message = PasswordMismatchMessage });

        // 4. Store the new hash; the repository also stamps PasswordUpdatedTime.
        var updated = await _repository.UpdatePasswordAsync(userId, PasswordHasher.Hash(request.NewPassword));
        if (!updated)
            return NotFound();

        return NoContent();
    }
}
