using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

/// <summary>
/// Login endpoint. Marked <see cref="AllowAnonymousAttribute"/> so it stays reachable without a
/// token even though authorization is required globally for every other controller.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Generic message on every failure so we never reveal which check (user, active, password) failed.
    private const string InvalidCredentialsMessage = "invalid credentials";

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
}
