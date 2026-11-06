using Shortly.Core.Abstractions.Data;

namespace Shortly.Core.UnitTests.TestData;

internal static class SqlFactoryData
{
    /// <summary>A SQL factory whose connection must never be opened unless authorization succeeded.</summary>
    public static ISqlConnectionFactory ThatSignalsAccess()
    {
        var factory = Substitute.For<ISqlConnectionFactory>();
        factory.CreateConnection().Returns(_ => throw new SqlReachedException());
        return factory;
    }
}

/// <summary>Thrown by the fake SQL factory: proves the handler got past authorization and reached the database.</summary>
internal sealed class SqlReachedException : Exception;
