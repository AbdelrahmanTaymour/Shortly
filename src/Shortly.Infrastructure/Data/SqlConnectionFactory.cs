using System.Data;
using Npgsql;
using Shortly.Core.Abstractions.Data;

namespace Shortly.Infrastructure.Data;

internal sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        return connection;
    }
}