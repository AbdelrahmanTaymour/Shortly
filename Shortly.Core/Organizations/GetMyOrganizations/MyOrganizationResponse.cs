using Shortly.Domain.Members;

namespace Shortly.Core.Organizations.GetMyOrganizations;

public sealed record MyOrganizationResponse(Guid OrganizationId, string Name, MemberRole Role, bool IsActive);