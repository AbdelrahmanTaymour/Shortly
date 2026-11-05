using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Domain.UnitTests.TestData;

internal static class InvitationData
{
    public static readonly TimeSpan Validity = TimeSpan.FromDays(7);

    public static OrganizationInvitation CreateInvitation(
        string email = "invitee@example.com",
        MemberRole role = MemberRole.Member,
        string tokenHash = "token-hash",
        Guid? organizationId = null,
        Guid? invitedBy = null) =>
        OrganizationInvitation.Create(organizationId ?? Guid.NewGuid(), email, role, invitedBy ?? Guid.NewGuid(),
            tokenHash, TestClock.Now.Add(Validity), TestClock.Now).Value;

    public static OrganizationInvitation CreateAcceptedInvitation()
    {
        var invitation = CreateInvitation();
        invitation.Accept(TestClock.Now);
        return invitation;
    }

    public static OrganizationInvitation CreateCancelledInvitation()
    {
        var invitation = CreateInvitation();
        invitation.Cancel(TestClock.Now);
        return invitation;
    }

    /// <summary>A pending invitation that has been marked expired by the clean-up job.</summary>
    public static OrganizationInvitation CreateExpiredInvitation()
    {
        var invitation = CreateInvitation();
        invitation.MarkExpired(TestClock.Now.Add(Validity));
        return invitation;
    }
}
