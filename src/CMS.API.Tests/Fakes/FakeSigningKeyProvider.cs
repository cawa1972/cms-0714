using CMS.API.Security;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="ISigningKeyProvider"/> returning a fixed key (or null).</summary>
public sealed class FakeSigningKeyProvider : ISigningKeyProvider
{
    private readonly string? _key;

    public FakeSigningKeyProvider(string? key) => _key = key;

    public string? GetSigningKey() => _key;
}
