using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.DeleteShortUrl;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class DeleteShortUrlCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IOrganizationAuthorizationService _organizationAuthorization = Substitute.For<IOrganizationAuthorizationService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private Task<Result> Handle(ShortUrl? link, IUserContext caller)
    {
        _shortUrls.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(link);
        var sut = new DeleteShortUrlCommandHandler(_shortUrls, caller, _organizationAuthorization, _unitOfWork);
        return sut.Handle(new DeleteShortUrlCommand(1), default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheLinkDoesNotExist()
    {
        // Act
        var result = await Handle(null, CallerData.Create(_userId));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndDeleteNothing_When_TheLinkBelongsToSomeoneElse()
    {
        // Arrange
        var link = ShortUrlData.UserLink(Guid.NewGuid());

        // Act
        var result = await Handle(link, CallerData.Create(_userId));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        _shortUrls.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_RemoveTheLink_AndSave_When_TheCallerOwnsIt()
    {
        // Arrange
        var link = ShortUrlData.UserLink(_userId);

        // Act
        var result = await Handle(link, CallerData.Create(_userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        _shortUrls.Received(1).Remove(link);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RemoveTheLink_When_TheCallerIsASystemAdmin()
    {
        // Arrange
        var link = ShortUrlData.UserLink(Guid.NewGuid());

        // Act
        var result = await Handle(link, CallerData.Create(_userId, isAdmin: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        _shortUrls.Received(1).Remove(link);
    }

    [Fact]
    public async Task Handle_Should_RemoveAnOrganizationLink_When_TheCallerIsAnActiveMemberOfThatOrganization()
    {
        // Arrange
        _organizationAuthorization.RequireMembershipAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(MemberData.Create(_orgId, _userId, MemberRole.Member)));
        var link = ShortUrlData.OrganizationLink(_orgId);

        // Act
        var result = await Handle(link, CallerData.Create(_userId, organizationId: _orgId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        _shortUrls.Received(1).Remove(link);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndDeleteNothing_When_AMemberWhoWasRemovedStillHoldsTheOrganizationClaim()
    {
        // Arrange
        _organizationAuthorization.RequireMembershipAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<OrganizationMember>(OrganizationErrors.NotAMember));

        // Act
        var result = await Handle(ShortUrlData.OrganizationLink(_orgId), CallerData.Create(_userId, organizationId: _orgId));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        _shortUrls.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndDeleteNothing_When_TheCallerIsAViewer()
    {
        // Arrange
        _organizationAuthorization.RequireMembershipAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(MemberData.Create(_orgId, _userId, MemberRole.Viewer)));

        // Act
        var result = await Handle(ShortUrlData.OrganizationLink(_orgId), CallerData.Create(_userId, organizationId: _orgId));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        _shortUrls.DidNotReceiveWithAnyArgs().Remove(default!);
    }
}
