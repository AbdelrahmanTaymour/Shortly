using Shortly.Domain.Members;

namespace Shortly.API.Controllers.Members;

public sealed record AddMemberRequest(Guid UserId, MemberRole Role);
