using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Members;

namespace Shortly.Core.Organizations.GetMyOrganizations;

internal sealed class GetMyOrganizationsQueryHandler(
    IUserContext userContext,
    IOrganizationMemberRepository memberRepository)
    : IQueryHandler<GetMyOrganizationsQuery, IReadOnlyList<MyOrganizationResponse>>
{
    public async Task<Result<IReadOnlyList<MyOrganizationResponse>>> Handle(
        GetMyOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var memberships = await memberRepository.GetByUserIdAsync(userContext.UserId, cancellationToken);

        var result = memberships
            .Select(m => new MyOrganizationResponse(m.OrganizationId, m.Organization!.Name, m.Role, m.IsActive))
            .ToList();

        return result;
    }
}