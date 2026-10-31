namespace Shortly.Core.Teams.GetTeamMembers;

public sealed record OrganizationTeamMemberResponse(Guid MemberId, string Username, string Email, DateTime JoinedAt);
