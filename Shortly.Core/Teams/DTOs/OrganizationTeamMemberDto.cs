namespace Shortly.Core.Teams.DTOs;

public record OrganizationTeamMemberDto(Guid Id, Guid TeamId, Guid MemberId, DateTime JoinedAt);