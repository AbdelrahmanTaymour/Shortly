using Shortly.Core.Abstractions.Authentication;

namespace Shortly.Core.UnitTests.TestData;

internal static class CallerData
{
    /// <summary>An authenticated caller. Wire <c>IsOwner</c> yourself when a handler depends on it.</summary>
    public static IUserContext Create(Guid userId, bool isAdmin = false, Guid? organizationId = null)
    {
        var context = Substitute.For<IUserContext>();
        context.UserId.Returns(userId);
        context.IsAuthenticated.Returns(true);
        context.IsAdmin.Returns(isAdmin);
        context.OrganizationId.Returns(organizationId);
        return context;
    }
}
