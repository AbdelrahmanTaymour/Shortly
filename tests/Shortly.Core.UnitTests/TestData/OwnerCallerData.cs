using Shortly.Core.Abstractions.Authentication;

namespace Shortly.Core.UnitTests.TestData;

internal static class OwnerCallerData
{
    /// <summary>A caller whose <c>IsOwner</c> behaves like the real user context: true only for its own id.</summary>
    public static IUserContext Create(Guid userId, bool isAdmin = false)
    {
        var caller = CallerData.Create(userId, isAdmin);
        caller.IsOwner(Arg.Any<Guid>()).Returns(call => call.Arg<Guid>() == userId);
        return caller;
    }
}
