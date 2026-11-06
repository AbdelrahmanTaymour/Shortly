using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Core.UnitTests.TestData;

internal static class InvitationData
{
    public const string Email = "invitee@example.com";
    public const string TokenHash = "token-hash";

    public static OrganizationInvitation Pending(
        Guid organizationId, Guid? invitedBy = null, string email = Email, MemberRole role = MemberRole.Member,
        DateTime? expiresAt = null) =>
        OrganizationInvitation.Create(organizationId, email, role, invitedBy ?? Guid.NewGuid(), TokenHash,
            expiresAt ?? TestClock.UtcNow.AddDays(7), TestClock.UtcNow.AddMinutes(-1)).Value;
}
