using Shortly.Core.Admin.ResetUserUsage;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class ResetUserUsageCommandHandlerTests
{
    private readonly User _user = UserData.Create();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResetUserUsageCommandHandler _sut;

    public ResetUserUsageCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _sut = new ResetUserUsageCommandHandler(_users, _unitOfWork, TestClock.Provider());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheUserDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new ResetUserUsageCommand(Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ResetTheMonthlyCounters_KeepTheTotals_AndSave()
    {
        // Arrange
        _user.Usage.IncrementLinksCreated(TestClock.UtcNow);
        _user.Usage.IncrementQrCodesCreated(TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(new ResetUserUsageCommand(_user.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.Usage.MonthlyLinksCreated.Should().Be(0);
        _user.Usage.MonthlyQrCodesCreated.Should().Be(0);
        _user.Usage.TotalLinksCreated.Should().Be(1);
        _user.Usage.TotalQrCodesCreated.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
