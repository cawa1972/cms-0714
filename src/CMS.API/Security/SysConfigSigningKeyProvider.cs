using System.Text.Json;
using CMS.API.Data;
using Dapper;

namespace CMS.API.Security;

/// <summary>
/// Reads the signing secret from <c>SysConfig</c> (configKey = 'appConfig', a JSON blob whose
/// <c>symmetricSecurityKey</c> property holds the value) and caches it — the secret is fixed for the
/// process lifetime, so it is fetched once and reused for every subsequent token validation.
/// </summary>
public sealed class SysConfigSigningKeyProvider : ISigningKeyProvider
{
    private readonly ISqlConnectionFactory _factory;
    private readonly object _gate = new();
    private string? _cached;

    public SysConfigSigningKeyProvider(ISqlConnectionFactory factory) => _factory = factory;

    public string? GetSigningKey()
    {
        if (_cached is not null)
            return _cached;

        lock (_gate)
        {
            if (_cached is not null)
                return _cached;

            using var db = _factory.Create();
            var configValue = db.ExecuteScalar<string?>(
                "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'");

            if (string.IsNullOrWhiteSpace(configValue))
                return null;

            using var doc = JsonDocument.Parse(configValue);
            if (doc.RootElement.TryGetProperty("symmetricSecurityKey", out var prop) &&
                prop.GetString() is { Length: > 0 } key)
            {
                _cached = key;
            }

            return _cached;
        }
    }
}
