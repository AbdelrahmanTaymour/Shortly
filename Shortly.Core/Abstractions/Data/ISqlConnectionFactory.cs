using System.Data;

namespace Shortly.Core.Abstractions.Data;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}