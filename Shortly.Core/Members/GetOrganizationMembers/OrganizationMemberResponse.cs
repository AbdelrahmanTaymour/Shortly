using Shortly.Domain.Members;

namespace Shortly.Core.Members.GetOrganizationMembers;

public sealed record OrganizationMemberResponse(Guid UserId, string Username, string Email, MemberRole Role, bool IsActive, DateTime JoinedAt);
