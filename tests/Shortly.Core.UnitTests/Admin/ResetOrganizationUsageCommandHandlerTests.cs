using Shortly.Core.Admin.ResetOrganizationUsage;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Admin;

public class ResetOrganizationUsageCommandHandlerTests
{
    private readonly Organization _organization = OrganizationData.Create();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResetOrganizationUsageCommandHandler _sut;

    public ResetOrganizationUsageCommandHandlerTests()
    {
        _organizations.GetByIdAsync(_organization.Id, Arg.Any<CancellationToken>()).Returns(_organization);
        _sut = new ResetOrganizationUsageCommandHandler(_organizations, _unitOfWork, TestClock.Provider());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new ResetOrganizationUsageCommand(Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ResetTheMonthlyCounters_AndSave()
    {
        // Arrange
        _organization.Usage.IncrementLinksCreated(TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(new ResetOrganizationUsageCommand(_organization.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _organization.Usage.MonthlyLinksCreated.Should().Be(0);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
