using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.TestData;

/// <summary>
/// A stand-in for <see cref="IOrganizationAuthorizationService"/> so handler tests state who the caller is
/// without re-testing the policy itself (that has its own tests).
/// </summary>
internal sealed class AuthorizationData
{
    public IOrganizationAuthorizationService Service { get; } = Substitute.For<IOrganizationAuthorizationService>();

    public OrganizationMember? Caller { get; private set; }

    public AuthorizationData CallerIs(Guid organizationId, Guid userId, MemberRole role)
    {
        var member = MemberData.Create(organizationId, userId, role);
        Caller = member;
        Service.RequireMembershipAsync(organizationId, Arg.Any<CancellationToken>()).Returns(Result.Success(member));
        Service.RequireRoleAsync(organizationId, Arg.Any<MemberRole>(), Arg.Any<CancellationToken>())
            .Returns(call => role >= call.ArgAt<MemberRole>(1)
                ? Result.Success(member)
                : Result.Failure<OrganizationMember>(OrganizationMemberErrors.InsufficientRole));
        return this;
    }

    public AuthorizationData CallerIsNotAMember(Guid organizationId)
    {
        Service.RequireMembershipAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<OrganizationMember>(OrganizationErrors.NotAMember));
        Service.RequireRoleAsync(organizationId, Arg.Any<MemberRole>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<OrganizationMember>(OrganizationErrors.NotAMember));
        return this;
    }
}
