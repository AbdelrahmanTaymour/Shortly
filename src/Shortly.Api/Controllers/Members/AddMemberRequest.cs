using Shortly.Domain.Members;

namespace Shortly.Api.Controllers.Members;

public sealed record AddMemberRequest(Guid UserId, MemberRole Role);
