using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Users.GetLoggedInUser;

public sealed record GetLoggedInUserQuery : IQuery<LoggedInUserResponse>;
