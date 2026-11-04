using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.RemoveMember;

internal sealed class RemoveMemberCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization, IOrganizationMemberRepository memberRepository, IUnitOfWork unitOfWork) : ICommandHandler<RemoveMemberCommand>
{
    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var caller = await organizationAuthorization.RequireRoleAsync(request.OrganizationId, MemberRole.OrgAdmin, cancellationToken);
        if (caller.IsFailure) return Result.Failure(caller.Error);

        var target = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, request.UserId, cancellationToken);
        if (target is null) return Result.Failure(OrganizationMemberErrors.NotFound);

        var result = target.Deactivate(); // guards OrgOwner internally — no separate "is owner" check needed
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}