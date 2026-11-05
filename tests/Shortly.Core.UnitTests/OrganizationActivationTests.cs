using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Organizations.ActivateOrganization;
using Shortly.Core.Organizations.DeactivateOrganization;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.UnitTests;

public class OrganizationActivationTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public OrganizationActivationTests() => _clock.UtcNow.Returns(TestData.UtcNow);

    private Organization ActiveOrganization()
    {
        var organization = Organization.Create("Acme", null, null, null, 10, _ownerId, enSubscriptionPlan.Free, TestData.UtcNow).Value;
        _organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);
        return organization;
    }

    private Organization InactiveOrganization()
    {
        var organization = ActiveOrganization();
        organization.Deactivate(TestData.UtcNow);
        return organization;
    }

    private Task<Result> Activate(Guid orgId, Shortly.Core.Abstractions.Authentication.IUserContext caller) =>
        new ActivateOrganizationCommandHandler(_organizations, caller, _unitOfWork, _clock)
            .Handle(new ActivateOrganizationCommand(orgId), default);

    private Task<Result> Deactivate(Guid orgId, Shortly.Core.Abstractions.Authentication.IUserContext caller) =>
        new DeactivateOrganizationCommandHandler(_organizations, caller, _unitOfWork, _clock)
            .Handle(new DeactivateOrganizationCommand(orgId), default);

    // IsOwner is derived from the caller's id, so wire it the way the real UserContext does.
    private Shortly.Core.Abstractions.Authentication.IUserContext Caller(Guid userId, bool isAdmin)
    {
        var caller = TestData.Caller(userId, isAdmin);
        caller.IsOwner(Arg.Any<Guid>()).Returns(call => call.Arg<Guid>() == userId);
        return caller;
    }

    [Fact]
    public async Task Deactivate_ByOwnerWhoIsNotSystemAdmin_Succeeds()
    {
        var organization = ActiveOrganization();

        var result = await Deactivate(organization.Id, Caller(_ownerId, isAdmin: false));

        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_BySystemAdminWhoIsNotOwner_Succeeds()
    {
        var organization = ActiveOrganization();

        var result = await Deactivate(organization.Id, Caller(Guid.NewGuid(), isAdmin: true));

        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_ByOtherUser_IsDeniedAndOrganizationUnchanged()
    {
        var organization = ActiveOrganization();

        var result = await Deactivate(organization.Id, Caller(Guid.NewGuid(), isAdmin: false));

        result.Error.Should().Be(OrganizationErrors.NotOwner);
        organization.IsActive.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Activate_ByOwnerWhoIsNotSystemAdmin_Succeeds()
    {
        var organization = InactiveOrganization();

        var result = await Activate(organization.Id, Caller(_ownerId, isAdmin: false));

        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Activate_BySystemAdminWhoIsNotOwner_Succeeds()
    {
        var organization = InactiveOrganization();

        var result = await Activate(organization.Id, Caller(Guid.NewGuid(), isAdmin: true));

        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Activate_ByOtherUser_IsDeniedAndOrganizationUnchanged()
    {
        var organization = InactiveOrganization();

        var result = await Activate(organization.Id, Caller(Guid.NewGuid(), isAdmin: false));

        result.Error.Should().Be(OrganizationErrors.NotOwner);
        organization.IsActive.Should().BeFalse();
    }
}
