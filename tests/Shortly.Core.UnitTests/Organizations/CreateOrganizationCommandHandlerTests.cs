using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Organizations.CreateOrganization;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Organizations;

public class CreateOrganizationCommandHandlerTests
{
    private static readonly AccessTokenResponse Tokens = new("access-token", "refresh-token", 900);

    private readonly User _user = UserData.Create();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly ISubscriptionPlanLimitsProvider _planLimits = Substitute.For<ISubscriptionPlanLimitsProvider>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateOrganizationCommandHandler _sut;

    public CreateOrganizationCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<OrganizationMember>(), Arg.Any<CancellationToken>()).Returns(Tokens);
        _planLimits.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>())
            .Returns(PlanLimitsData.With(maxMembers: 3, maxOrganizations: 2));
        _sut = new CreateOrganizationCommandHandler(
            CallerData.Create(_user.Id), _users, _organizations, _members, _jwtService, _planLimits, _unitOfWork,
            TestClock.Provider());
    }

    private static CreateOrganizationCommand Command(string name = "Acme") =>
        new(name, "Description", "https://acme.dev", "https://acme.dev/logo.png");

    [Fact]
    public async Task Handle_Should_ReturnNotAuthenticated_When_TheCallerNoLongerExists()
    {
        // Arrange
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.Error.Should().Be(AuthenticationErrors.NotAuthenticated);
        _organizations.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_CreateAFreePlanOrganizationOwnedByTheCaller()
    {
        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _organizations.Received(1).Add(Arg.Is<Organization>(o =>
            o.Name == "Acme" && o.OwnerId == _user.Id && o.SubscriptionPlanId == enSubscriptionPlan.Free
            && !o.IsSubscribed && o.IsActive && o.Description == "Description"));
    }

    [Fact]
    public async Task Handle_Should_TakeTheMemberLimitFromTheFreePlan()
    {
        // Act
        await _sut.Handle(Command(), default);

        // Assert
        await _planLimits.Received().GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>());
        _organizations.Received(1).Add(Arg.Is<Organization>(o => o.MemberLimit == 3));
    }

    [Fact]
    public async Task Handle_Should_UseAnEffectivelyUnlimitedMemberLimit_When_TheFreePlanIsUnlimited()
    {
        // Arrange
        _planLimits.GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>())
            .Returns(PlanLimitsData.With(maxMembers: -1));

        // Act
        await _sut.Handle(Command(), default);

        // Assert
        _organizations.Received(1).Add(Arg.Is<Organization>(o => o.MemberLimit == int.MaxValue));
    }

    [Fact]
    public async Task Handle_Should_ReturnOrganizationLimitReached_AndCreateNothing_When_TheUserOwnsTheirPlanQuota()
    {
        // Arrange
        _organizations.CountOwnedByAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(2);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.OrganizationLimitReached);
        _organizations.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Allow_TheLastOrganizationWithinTheQuota()
    {
        // Arrange
        _organizations.CountOwnedByAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_NotLimitOrganizations_When_TheUsersPlanIsUnlimited()
    {
        // Arrange
        _planLimits.GetLimitsAsync(_user.SubscriptionPlanId, Arg.Any<CancellationToken>())
            .Returns(PlanLimitsData.With(maxOrganizations: -1));
        _organizations.CountOwnedByAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(500);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _organizations.DidNotReceiveWithAnyArgs().CountOwnedByAsync(default, default);
    }

    [Fact]
    public async Task Handle_Should_TrimAndCollapseWhitespaceInTheName()
    {
        // Act
        await _sut.Handle(Command(name: "  Acme    Corp  "), default);

        // Assert
        _organizations.Received(1).Add(Arg.Is<Organization>(o => o.Name == "Acme Corp"));
    }

    [Fact]
    public async Task Handle_Should_MakeTheCallerTheOwnerMember()
    {
        // Act
        await _sut.Handle(Command(), default);

        // Assert
        _members.Received(1).Add(Arg.Is<OrganizationMember>(m =>
            m.UserId == _user.Id && m.Role == MemberRole.OrgOwner && m.IsActive));
    }

    [Fact]
    public async Task Handle_Should_SaveBeforeIssuingTokensForTheNewOrganization()
    {
        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.Value.Should().Be(Tokens);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _jwtService.IssueTokensAsync(_user, Arg.Is<OrganizationMember>(m => m.Role == MemberRole.OrgOwner), Arg.Any<CancellationToken>());
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_Should_ReturnNameRequired_AndCreateNothing_When_TheNameIsBlank(string name)
    {
        // Act
        var result = await _sut.Handle(Command(name: name), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NameRequired);
        _organizations.DidNotReceiveWithAnyArgs().Add(default!);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
