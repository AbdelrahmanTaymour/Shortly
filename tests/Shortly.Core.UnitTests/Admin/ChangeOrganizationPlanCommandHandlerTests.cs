using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Admin.ChangeOrganizationPlan;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.UnitTests.Admin;

public class ChangeOrganizationPlanCommandHandlerTests
{
    private readonly Organization _organization = OrganizationData.Create(memberLimit: 3);
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly ISubscriptionPlanLimitsProvider _planLimits = Substitute.For<ISubscriptionPlanLimitsProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ChangeOrganizationPlanCommandHandler _sut;

    public ChangeOrganizationPlanCommandHandlerTests()
    {
        _organizations.GetByIdAsync(_organization.Id, Arg.Any<CancellationToken>()).Returns(_organization);
        _planLimits.GetLimitsAsync(enSubscriptionPlan.Professional, Arg.Any<CancellationToken>())
            .Returns(PlanLimitsData.With(maxMembers: 50));
        _sut = new ChangeOrganizationPlanCommandHandler(_organizations, _planLimits, _unitOfWork, TestClock.Provider());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new ChangeOrganizationPlanCommand(Guid.NewGuid(), enSubscriptionPlan.Professional), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ChangeThePlan_SetTheMemberLimitFromIt_AndSave()
    {
        // Act
        var result = await _sut.Handle(new ChangeOrganizationPlanCommand(_organization.Id, enSubscriptionPlan.Professional), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _organization.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Professional);
        _organization.IsSubscribed.Should().BeTrue();
        _organization.MemberLimit.Should().Be(50);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UseAnEffectivelyUnlimitedMemberLimit_When_ThePlanIsUnlimited()
    {
        // Arrange
        _planLimits.GetLimitsAsync(enSubscriptionPlan.Enterprise, Arg.Any<CancellationToken>())
            .Returns(PlanLimitsData.With(maxMembers: -1));

        // Act
        await _sut.Handle(new ChangeOrganizationPlanCommand(_organization.Id, enSubscriptionPlan.Enterprise), default);

        // Assert
        _organization.MemberLimit.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task Handle_Should_LowerTheLimit_WhenMovingToASmallerPlan()
    {
        // Arrange
        _planLimits.GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>())
            .Returns(PlanLimitsData.With(maxMembers: 3));
        await _sut.Handle(new ChangeOrganizationPlanCommand(_organization.Id, enSubscriptionPlan.Professional), default);

        // Act
        await _sut.Handle(new ChangeOrganizationPlanCommand(_organization.Id, enSubscriptionPlan.Free), default);

        // Assert
        _organization.MemberLimit.Should().Be(3);
        _organization.IsSubscribed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidPlan_AndSaveNothing_When_ThePlanIsNotDefined()
    {
        // Act
        var result = await _sut.Handle(new ChangeOrganizationPlanCommand(_organization.Id, (enSubscriptionPlan)99), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.InvalidSubscriptionPlan);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _planLimits.DidNotReceiveWithAnyArgs().GetLimitsAsync(default, default);
    }
}
