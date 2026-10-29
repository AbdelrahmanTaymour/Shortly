using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.CreateUser;

public sealed record CreateUserCommand(string Email, string Username, string Password, UserRole Role) : ICommand<Guid>;
