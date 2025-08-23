namespace Shortly.Core.DTOs.OrganizationDTOs;

public record OrganizationTeamMemberDto(Guid Id, Guid TeamId, Guid MemberId, DateTime JoinedAt);