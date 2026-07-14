using System.Data;
using Microsoft.Data.SqlClient;

namespace CMS.API.Data;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("CMS")
            ?? throw new InvalidOperationException("Connection string 'CMS' is not configured.");
    }

    public IDbConnection Create() => new SqlConnection(_connectionString);
}
