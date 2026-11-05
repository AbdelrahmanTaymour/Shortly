using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Domain.Members;

namespace Shortly.Core.UnitTests;

internal static class TestData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static OrganizationMember Member(Guid organizationId, Guid userId, MemberRole role, bool isActive = true)
    {
        var member = role == MemberRole.OrgOwner
            ? OrganizationMember.CreateOwner(organizationId, userId, UtcNow)
            : OrganizationMember.Invite(organizationId, userId, role, Guid.NewGuid(), UtcNow).Value;

        if (!isActive) member.Deactivate();
        return member;
    }

    public static IUserContext Caller(Guid userId, bool isAdmin = false, Guid? organizationId = null)
    {
        var context = Substitute.For<IUserContext>();
        context.UserId.Returns(userId);
        context.IsAuthenticated.Returns(true);
        context.IsAdmin.Returns(isAdmin);
        context.OrganizationId.Returns(organizationId);
        return context;
    }

    /// <summary>A SQL factory whose connection must never be opened unless authorization succeeded.</summary>
    public static ISqlConnectionFactory SqlFactoryThatSignalsAccess()
    {
        var factory = Substitute.For<ISqlConnectionFactory>();
        factory.CreateConnection().Returns(_ => throw new SqlReachedException());
        return factory;
    }
}

/// <summary>Thrown by the fake SQL factory: proves the handler got past authorization and reached the database.</summary>
internal sealed class SqlReachedException : Exception;
