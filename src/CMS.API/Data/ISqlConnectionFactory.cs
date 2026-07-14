using System.Data;

namespace CMS.API.Data;

/// <summary>
/// Creates ADO.NET connections to the CMS database. Abstracted so repositories
/// can be unit-tested against a fake and so the connection string lives in one place.
/// </summary>
public interface ISqlConnectionFactory
{
    IDbConnection Create();
}
