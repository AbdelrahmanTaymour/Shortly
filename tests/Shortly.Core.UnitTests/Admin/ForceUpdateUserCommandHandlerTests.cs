using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Admin.ForceUpdateUser;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class ForceUpdateUserCommandHandlerTests
{
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly User _user = UserData.Create();
    private readonly IUserActionTokenRepository _tokens = Substitute.For<IUserActionTokenRepository>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IEmailNotifier _notifier = Substitute.For<IEmailNotifier>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ForceUpdateUserCommandHandler _sut;

    public ForceUpdateUserCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _tokenHasher.GenerateToken().Returns("plain-token");
        _tokenHasher.Hash("plain-token").Returns("hashed-token");
        _sut = new ForceUpdateUserCommandHandler(
            CallerData.Create(_adminId, isAdmin: true), _users, _tokens, _tokenHasher, _notifier, _unitOfWork, TestClock.Provider());
    }

    private ForceUpdateUserCommand Command(
        string? email = null, string? username = null, UserRole? role = null, enSubscriptionPlan? plan = null,
        bool? isActive = null, string? name = null, string? bio = null, string? phone = null, string? company = null,
        string? location = null, string? country = null, string? timeZone = null) =>
        new(_user.Id, email, username, role, plan, isActive, name, bio, phone, company, location, country, timeZone);

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheUserDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(Command() with { UserId = Guid.NewGuid() }, default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ChangeNothing_ButStillSucceed_When_NoFieldIsGiven()
    {
        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.Email.Should().Be(UserData.Email);
        _user.Username.Should().Be("jane");
        _user.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task Handle_Should_UpdateEveryGivenField_AndSave()
    {
        // Act
        var result = await _sut.Handle(Command("new@example.com", "newname", UserRole.SuperAdmin, enSubscriptionPlan.Professional,
            isActive: false, name: "Jane Doe", bio: "Bio", phone: "123", company: "Acme", location: "Cairo", country: "EG",
            timeZone: "UTC"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.Email.Should().Be("new@example.com");
        _user.Username.Should().Be("newname");
        _user.Role.Should().Be(UserRole.SuperAdmin);
        _user.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Professional);
        _user.IsActive.Should().BeFalse();
        _user.Profile.Name.Should().Be("Jane Doe");
        _user.Profile.Bio.Should().Be("Bio");
        _user.Profile.PhoneNumber.Should().Be("123");
        _user.Profile.Company.Should().Be("Acme");
        _user.Profile.Location.Should().Be("Cairo");
        _user.Profile.Country.Should().Be("EG");
        _user.Profile.TimeZone.Should().Be("UTC");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotUnique_AndSaveNothing_When_TheNewEmailIsTaken()
    {
        // Arrange
        _users.EmailExistsAsync("taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(Command(email: "taken@example.com"), default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotUnique_AndSaveNothing_When_TheNewUsernameIsTaken()
    {
        // Arrange
        _users.UsernameExistsAsync("taken", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(Command(username: "taken"), default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotCheckUniqueness_When_TheEmailAndUsernameAreUnchanged()
    {
        // Act
        var result = await _sut.Handle(Command(email: UserData.Email, username: "jane"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _users.DidNotReceiveWithAnyArgs().EmailExistsAsync(default!, default);
        await _users.DidNotReceiveWithAnyArgs().UsernameExistsAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_TreatActivatingAnActiveUser_AndDeactivatingAnInactiveOne_AsSuccess()
    {
        // Act
        var activate = await _sut.Handle(Command(isActive: true), default);
        await _sut.Handle(Command(isActive: false), default);
        var deactivate = await _sut.Handle(Command(isActive: false), default);

        // Assert
        activate.IsSuccess.Should().BeTrue();
        deactivate.IsSuccess.Should().BeTrue();
        _user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnDeleted_When_ActivatingADeletedUser()
    {
        // Arrange
        _user.SoftDelete(Guid.NewGuid(), TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(Command(isActive: true), default);

        // Assert
        result.Error.Should().Be(UserErrors.Deleted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_KeepTheOtherProfileFields_When_OnlyOneIsGiven()
    {
        // Arrange
        _user.Profile.Update("Old name", "Old bio", "1", "https://old.dev", "Old Co", "Old city", "EG", "UTC", TestClock.UtcNow);

        // Act
        await _sut.Handle(Command(company: "New Co"), default);

        // Assert
        _user.Profile.Company.Should().Be("New Co");
        _user.Profile.Name.Should().Be("Old name");
        _user.Profile.Website.Should().Be("https://old.dev");
    }

    [Fact]
    public async Task Handle_Should_KeepEveryOmittedProfileField_WhenAnotherOneChanges()
    {
        // Arrange
        _user.Profile.Update("Old name", "Old bio", "1", "https://old.dev", "Old Co", "Old city", "EG", "UTC", TestClock.UtcNow);

        // Act
        await _sut.Handle(Command(name: "New name"), default);

        // Assert
        _user.Profile.Name.Should().Be("New name");
        _user.Profile.Bio.Should().Be("Old bio");
        _user.Profile.PhoneNumber.Should().Be("1");
        _user.Profile.Company.Should().Be("Old Co");
        _user.Profile.Location.Should().Be("Old city");
        _user.Profile.Country.Should().Be("EG");
        _user.Profile.TimeZone.Should().Be("UTC");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_Should_ClearAProfileField_When_ItIsSentBlank(string blank)
    {
        // Arrange
        _user.Profile.Update("Old name", "Old bio", null, null, null, null, null, null, TestClock.UtcNow);

        // Act
        await _sut.Handle(Command(bio: blank), default);

        // Assert
        _user.Profile.Bio.Should().BeNull();
        _user.Profile.Name.Should().Be("Old name");
    }

    [Fact]
    public async Task Handle_Should_LeaveTheEmailUnconfirmed_IssueAFreshToken_AndEmailItAfterSaving_When_TheEmailChanges()
    {
        // Arrange
        var confirmed = UserData.CreateConfirmed();
        _users.GetByIdAsync(confirmed.Id, Arg.Any<CancellationToken>()).Returns(confirmed);

        // Act
        var result = await _sut.Handle(Command(email: "new@example.com") with { UserId = confirmed.Id }, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        confirmed.Email.Should().Be("new@example.com");
        confirmed.IsEmailConfirmed.Should().BeFalse();
        await _tokens.Received(1).InvalidateActiveTokensAsync(confirmed.Id, enUserActionTokenType.EmailVerification,
            TestClock.UtcNow, Arg.Any<CancellationToken>());
        _tokens.Received(1).Add(Arg.Is<UserActionToken>(t =>
            t.UserId == confirmed.Id && t.TokenHash == "hashed-token" && t.TokenType == enUserActionTokenType.EmailVerification));
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _notifier.EnqueueSendEmailVerificationAsync("new@example.com", confirmed.Username, "plain-token");
        });
    }

    [Fact]
    public async Task Handle_Should_NotSendAnyEmail_When_TheEmailDoesNotChange()
    {
        // Act
        await _sut.Handle(Command(name: "Jane"), default);

        // Assert
        _tokens.DidNotReceiveWithAnyArgs().Add(default!);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueSendEmailVerificationAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Handle_Should_NotSendTheEmail_When_SavingIsRejectedBeforeIt()
    {
        // Arrange
        _users.UsernameExistsAsync("taken", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(Command(email: "new@example.com", username: "taken"), default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueSendEmailVerificationAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Handle_Should_RefuseAnAdministratorWhoDeactivatesThemselves()
    {
        // Arrange
        var self = UserData.CreateWithId(_adminId);
        _users.GetByIdAsync(_adminId, Arg.Any<CancellationToken>()).Returns(self);

        // Act
        var result = await _sut.Handle(Command(isActive: false) with { UserId = _adminId }, default);

        // Assert
        result.Error.Should().Be(UserErrors.CannotTargetSelf);
        self.IsActive.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RefuseAnAdministratorWhoChangesTheirOwnRole()
    {
        // Arrange
        var self = UserData.CreateWithId(_adminId);
        _users.GetByIdAsync(_adminId, Arg.Any<CancellationToken>()).Returns(self);

        // Act
        var result = await _sut.Handle(Command(role: UserRole.SystemAdmin) with { UserId = _adminId }, default);

        // Assert
        result.Error.Should().Be(UserErrors.CannotTargetSelf);
        self.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task Handle_Should_StillAllowAnAdministratorToUpdateTheirOwnProfile_AndKeepTheirRoleAndState()
    {
        // Arrange
        var self = UserData.CreateWithId(_adminId);
        _users.GetByIdAsync(_adminId, Arg.Any<CancellationToken>()).Returns(self);

        // Act
        var result = await _sut.Handle(Command(role: UserRole.User, isActive: true, name: "Admin") with { UserId = _adminId }, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        self.Profile.Name.Should().Be("Admin");
    }
}
