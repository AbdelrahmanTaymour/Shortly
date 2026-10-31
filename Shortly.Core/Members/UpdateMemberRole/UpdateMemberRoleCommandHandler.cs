using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.UpdateMemberRole;

internal sealed class UpdateMemberRoleCommandHandler(
    IUserContext userContext, IOrganizationMemberRepository memberRepository, IUnitOfWork unitOfWork) : ICommandHandler<UpdateMemberRoleCommand>
{
    public async Task<Result> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var caller = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, userContext.UserId, cancellationToken);
        if (caller is null || caller.Role < MemberRole.OrgAdmin)
            return Result.Failure(OrganizationMemberErrors.InsufficientRole);

        var target = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, request.UserId, cancellationToken);
        if (target is null) return Result.Failure(OrganizationMemberErrors.NotFound);

        var result = target.ChangeRole(request.NewRole);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}